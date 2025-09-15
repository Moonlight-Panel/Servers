using System.Collections.Concurrent;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSystem;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

public class ServerService : IHostedLifecycleService
{
    private readonly ConcurrentDictionary<int, Server> Servers = new();
    private readonly ILogger<ServerService> Logger;
    private readonly ServerFactory ServerFactory;
    private readonly RemoteService RemoteService;
    private readonly ServerConfigurationMapper ConfigurationMapper;

    public ServerService(
        ILogger<ServerService> logger,
        ServerFactory serverFactory,
        RemoteService remoteService,
        ServerConfigurationMapper configurationMapper
    )
    {
        Logger = logger;
        ServerFactory = serverFactory;
        RemoteService = remoteService;
        ConfigurationMapper = configurationMapper;
    }

    public Server? GetById(int id)
        => Servers.GetValueOrDefault(id);

    public async Task Initialize(ServerConfiguration configuration)
    {
        var existingServer = Servers.GetValueOrDefault(configuration.Id);

        if (existingServer != null)
        {
            existingServer.Context.Configuration = configuration;
            // TODO: Implement a way for components to get notified
        }
        else
        {
            var server = await ServerFactory.CreateAsync(configuration);
            Servers[configuration.Id] = server;

            await server.InitializeAsync();
        }
    }

    public async Task InitializeById(int id)
    {
        var serverData = await RemoteService.GetServer(id);
        var config = ConfigurationMapper.FromServerDataResponse(serverData);

        await Initialize(config);
    }

    private async Task InitializeAll()
    {
        Logger.LogDebug("Initialing servers from panel");

        var servers = await PagedData<ServerDataResponse>.All(async (page, pageSize) =>
            await RemoteService.GetServers(page, pageSize)
        );

        foreach (var serverData in servers)
        {
            try
            {
                var config = ConfigurationMapper.FromServerDataResponse(serverData);

                await Initialize(config);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "An error occured while initializing server {id}", serverData.Id);
            }
        }
    }

    #region Lifetime handlers

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        await InitializeAll();
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Stopping server service. Disposing servers");

        foreach (var server in Servers.Values)
            await server.DisposeAsync();
    }

    #endregion
}