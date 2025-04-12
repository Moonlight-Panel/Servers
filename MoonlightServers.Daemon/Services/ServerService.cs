using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Models;
using MoonlightServers.Daemon.Abstractions;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerService : IHostedLifecycleService
{
    private readonly List<Server> Servers = new();
    private readonly ILogger<ServerService> Logger;
    private readonly RemoteService RemoteService;
    private readonly IServiceProvider ServiceProvider;
    private readonly ILoggerFactory LoggerFactory;
    private readonly IHubContext<ServerWebSocketHub> WebSocketHub;
    private CancellationTokenSource Cancellation = new();
    private bool IsInitialized = false;

    public ServerService(
        RemoteService remoteService,
        ILogger<ServerService> logger,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IHubContext<ServerWebSocketHub> webSocketHub
    )
    {
        RemoteService = remoteService;
        Logger = logger;
        ServiceProvider = serviceProvider;
        LoggerFactory = loggerFactory;
        WebSocketHub = webSocketHub;
    }

    public async Task Initialize() //TODO: Add initialize call from panel
    {
        if (IsInitialized)
        {
            Logger.LogWarning("Ignoring initialize call: Already initialized");
            return;
        }
        else
            IsInitialized = true;

        // Loading models and converting them
        Logger.LogInformation("Fetching servers from panel");

        var servers = await PagedData<ServerDataResponse>.All(async (page, pageSize) =>
            await RemoteService.GetServers(page, pageSize)
        );

        var configurations = servers
            .Select(x => x.ToServerConfiguration())
            .ToArray();

        Logger.LogInformation("Initializing {count} servers", servers.Length);

        await InitializeServerRange(configurations); // TODO: Initialize them multi threaded (maybe)

        // Attach to docker events
        await AttachToDockerEvents();
    }

    public async Task Stop()
    {
        Server[] servers;

        lock (Servers)
            servers = Servers.ToArray();

        //
        Logger.LogTrace("Canceling server tasks and disconnecting storage");

        foreach (var server in servers)
        {
            try
            {
                await server.CancelTasks();
                await server.DestroyStorage();
            }
            catch (Exception e)
            {
                Logger.LogCritical(
                    "An unhandled error occured while stopping the server management for server {id}: {e}",
                    server.Id,
                    e
                );
            }
        }

        // 
        Logger.LogTrace("Canceling own tasks");
        await Cancellation.CancelAsync();
    }

    private Task AttachToDockerEvents()
    {
        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        Task.Run(async () =>
        {
            // This lets the event monitor restart
            while (!Cancellation.Token.IsCancellationRequested)
            {
                try
                {
                    Logger.LogTrace("Attached to docker events");

                    await dockerClient.System.MonitorEventsAsync(new ContainerEventsParameters(),
                        new Progress<Message>(async message =>
                        {
                            if (message.Action != "die")
                                return;

                            Server? server;

                            // TODO: Maybe implement a lookup for containers which id isn't set in the cache

                            // Check if it's a runtime container
                            lock (Servers)
                                server = Servers.FirstOrDefault(x => x.RuntimeContainerId == message.ID);

                            if (server != null)
                            {
                                await server.NotifyRuntimeContainerDied();
                                return;
                            }

                            // Check if it's an installation container
                            lock (Servers)
                                server = Servers.FirstOrDefault(x => x.InstallationContainerId == message.ID);

                            if (server != null)
                            {
                                await server.NotifyInstallationContainerDied();
                                return;
                            }
                        }), Cancellation.Token);
                }
                catch (TaskCanceledException)
                {
                } // Can be ignored
                catch (Exception e)
                {
                    Logger.LogError("An unhandled error occured while attaching to docker events: {e}", e);
                }
            }
        });

        return Task.CompletedTask;
    }

    public async Task InitializeServerRange(ServerConfiguration[] serverConfigurations)
    {
        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        var existingContainers = await dockerClient.Containers.ListContainersAsync(new()
        {
            All = true,
            Limit = null,
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "label",
                    new Dictionary<string, bool>()
                    {
                        {
                            "Software=Moonlight-Panel",
                            true
                        }
                    }
                }
            }
        });

        foreach (var configuration in serverConfigurations)
            await InitializeServer(configuration, existingContainers);
    }

    public async Task<Server> InitializeServer(
        ServerConfiguration serverConfiguration,
        IList<ContainerListResponse> existingContainers
    )
    {
        Logger.LogTrace("Initializing server '{id}'", serverConfiguration.Id);

        var server = new Server(
            LoggerFactory.CreateLogger($"Server {serverConfiguration.Id}"),
            ServiceProvider,
            serverConfiguration,
            WebSocketHub
        );

        await server.Initialize(existingContainers);

        lock (Servers)
            Servers.Add(server);

        return server;
    }

    public async Task Sync(int serverId)
    {
        var serverData = await RemoteService.GetServer(serverId);
        var serverConfiguration = serverData.ToServerConfiguration();

        var server = GetServer(serverId);

        if (server == null)
            await InitializeServer(serverConfiguration, []);
        else
            server.UpdateConfiguration(serverConfiguration);
    }

    public async Task Delete(int serverId)
    {
        var server = GetServer(serverId);

        // If a server with this id doesn't exist we can just exit
        if (server == null)
            return;

        if (server.State == ServerState.Installing)
            throw new HttpApiException("Unable to delete a server while it is installing", 400);

        #region Callbacks

        var deleteCompletion = new TaskCompletionSource();

        async Task HandleStateChange(ServerState state)
        {
            if (state == ServerState.Offline)
                await DeleteServer();
        }

        async Task DeleteServer()
        {
            await server.CancelTasks();
            await server.DestroyStorage();
            await server.RemoveInstallationVolume();
            await server.RemoveRuntimeVolume();

            deleteCompletion.SetResult();

            lock (Servers)
                Servers.Remove(server);
        }

        #endregion

        // If the server is still online, we are killing it and then
        // waiting for the callback to trigger notifying us that the server is now offline
        // so we can delete it. The request will pause until then using the deleteCompletion task
        if (server.State != ServerState.Offline)
        {
            server.OnStateChanged += HandleStateChange;
            await server.Kill();

            await deleteCompletion.Task;
        }
        else
            await DeleteServer();
    }

    public Server? GetServer(int id)
    {
        lock (Servers)
            return Servers.FirstOrDefault(x => x.Id == id);
    }

    #region Lifecycle

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Initialize();
        }
        catch (Exception e)
        {
            Logger.LogCritical("Unable to initialize servers. Is the panel online? Error: {e}", e);
        }
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Stop();
        }
        catch (Exception e)
        {
            Logger.LogCritical("Unable to stop server handling: {e}", e);
        }
    }

    #endregion
}