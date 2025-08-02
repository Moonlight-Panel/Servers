using System.Collections.Concurrent;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSys;
using MoonlightServers.Daemon.ServerSystem;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;
using Server = MoonlightServers.Daemon.ServerSys.Abstractions.Server;

namespace MoonlightServers.Daemon.Services;

public class NewServerService : IHostedLifecycleService
{
    private readonly ILogger<ServerService> Logger;
    private readonly ServerFactory ServerFactory;
    private readonly RemoteService RemoteService;
    private readonly ServerConfigurationMapper Mapper;

    private readonly ConcurrentDictionary<int, Server> Servers = new();

    public NewServerService(
        ILogger<ServerService> logger,
        ServerFactory serverFactory,
        RemoteService remoteService,
        ServerConfigurationMapper mapper
    )
    {
        Logger = logger;
        ServerFactory = serverFactory;
        RemoteService = remoteService;
        Mapper = mapper;
    }

    public async Task InitializeAllFromPanel()
    {
        var servers = await PagedData<ServerDataResponse>.All(async (page, pageSize) =>
            await RemoteService.GetServers(page, pageSize)
        );

        foreach (var serverDataResponse in servers)
        {
            var configuration = Mapper.FromServerDataResponse(serverDataResponse);

            try
            {
                await Initialize(configuration);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "An error occured while initializing server: {id}", serverDataResponse.Id);
            }
        }
    }

    public async Task<Server> Initialize(ServerConfiguration serverConfiguration)
    {
        var server = ServerFactory.CreateServer(serverConfiguration);

        Servers[serverConfiguration.Id] = server;

        await server.Initialize();

        return server;
    }

    public Server? Find(int serverId)
        => Servers.GetValueOrDefault(serverId);

    public async Task Sync(int serverId)
    {
        var server = Find(serverId);

        if (server == null)
            throw new ArgumentException("No server with this id found", nameof(serverId));

        var serverData = await RemoteService.GetServer(serverId);
        var config = Mapper.FromServerDataResponse(serverData);

        server.Context.Configuration = config;

        await server.Sync();
    }

    public async Task Delete(int serverId)
    {
        var server = Find(serverId);

        if (server == null)
            throw new ArgumentException("No server with this id found", nameof(serverId));

        if (server.StateMachine.State == ServerState.Installing)
            throw new HttpApiException("Unable to delete a server while it is installing", 400);

        if (server.StateMachine.State != ServerState.Offline)
        {
            // If the server is not offline we need to wait until it goes offline, we
            // do that by creating the serverOfflineWaiter task completion source which will get triggered
            // when the event handler for state changes gets informed that the server state is now offline

            var serverOfflineWaiter = new TaskCompletionSource();
            var timeoutCancellation = new CancellationTokenSource();

            // Set timeout to 10 seconds, this gives the server 10 seconds to go offline, before the request fails
            timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(10));

            // Subscribe to state updates in order to get notified when the server is offline
            server.StateMachine.OnTransitioned(transition =>
            {
                // Only listen for changes to offline
                if (transition.Destination != ServerState.Offline)
                    return;

                // If the timeout has already been reached, ignore all changes
                if (timeoutCancellation.IsCancellationRequested)
                    return;

                // Server is finally offline, notify the request that we now can delete the server
                serverOfflineWaiter.SetResult();
            });

            // Now we trigger the kill and waiting for the server to be deleted
            await server.StateMachine.FireAsync(ServerTrigger.Kill);

            try
            {
                await serverOfflineWaiter.Task.WaitAsync(timeoutCancellation.Token);

                await DeleteServer_Unhandled(server);
            }
            catch (TaskCanceledException)
            {
                Logger.LogWarning(
                    "Deletion of server {id} failed because it didnt stop in time despite being killed",
                    server.Context.Configuration.Id
                );

                throw new HttpApiException(
                    "Could not kill the server in time for the deletion. Please try again later",
                    500
                );
            }
        }
        else
            await DeleteServer_Unhandled(server);
    }
    
    private async Task DeleteServer_Unhandled(Server server)
    {
        await server.Delete();
        await server.DisposeAsync();

        Servers.Remove(server.Context.Configuration.Id, out _);
    }

    #region Lifetime

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        await InitializeAllFromPanel();
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        foreach (var server in Servers.Values)
            await server.DisposeAsync();
    }

    #endregion
}