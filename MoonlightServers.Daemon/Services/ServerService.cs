using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Attributes;
using MoonCore.Models;
using MoonlightServers.Daemon.Abstractions;
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
    private bool IsInitialized = false;
    private CancellationTokenSource Cancellation = new();

    public ServerService(RemoteService remoteService, ILogger<ServerService> logger, IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        RemoteService = remoteService;
        Logger = logger;
        ServiceProvider = serviceProvider;
        LoggerFactory = loggerFactory;
    }

    public async Task Initialize() //TODO: Add initialize call from panel
    {
        if (IsInitialized)
        {
            Logger.LogWarning("Ignoring initialize call: Already initialized");
            return;
        }

        IsInitialized = true;

        // Loading models and converting them
        Logger.LogInformation("Fetching servers from panel");
        using var apiClient = await RemoteService.CreateHttpClient();

        var servers = await PagedData<ServerDataResponse>.All(async (page, pageSize) =>
            await apiClient.GetJson<PagedData<ServerDataResponse>>(
                $"api/servers/remote/servers?page={page}&pageSize={pageSize}"
            )
        );

        var configurations = servers.Select(x => new ServerConfiguration()
        {
            Id = x.Id,
            StartupCommand = x.StartupCommand,
            Allocations = x.Allocations.Select(y => new ServerConfiguration.AllocationConfiguration()
            {
                IpAddress = y.IpAddress,
                Port = y.Port
            }).ToArray(),
            Variables = x.Variables,
            OnlineDetection = x.OnlineDetection,
            DockerImage = x.DockerImage,
            UseVirtualDisk = x.UseVirtualDisk,
            Bandwidth = x.Bandwidth,
            Cpu = x.Cpu,
            Disk = x.Disk,
            Memory = x.Memory,
            StopCommand = x.StopCommand
        }).ToArray();

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
        Logger.LogTrace("Canceling server tasks");

        foreach (var server in servers)
            await server.CancelTasks();

        // 
        Logger.LogTrace("Canceling own tasks");
        await Cancellation.CancelAsync();
    }

    private async Task AttachToDockerEvents()
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

                            lock (Servers)
                                server = Servers.FirstOrDefault(x => x.RuntimeContainerId == message.ID || x.InstallationContainerId == message.ID);

                            // TODO: Maybe implement a lookup for containers which id isn't set in the cache

                            if (server == null)
                                return;

                            await server.NotifyContainerDied();
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
    }

    private async Task InitializeServerRange(ServerConfiguration[] serverConfigurations)
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

    private async Task InitializeServer(
        ServerConfiguration serverConfiguration,
        IList<ContainerListResponse> existingContainers
    )
    {
        Logger.LogTrace("Initializing server '{id}'", serverConfiguration.Id);

        var server = new Server(
            LoggerFactory.CreateLogger($"Server {serverConfiguration.Id}"),
            ServiceProvider,
            serverConfiguration
        );

        await server.Initialize(existingContainers);

        lock (Servers)
            Servers.Add(server);
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