using System.Collections.Concurrent;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSys;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

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

        await server.DisposeAsync();

        Servers.Remove(serverId, out _);
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