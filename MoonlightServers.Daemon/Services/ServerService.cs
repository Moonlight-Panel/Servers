using System.Collections.Concurrent;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MoonCore.Attributes;
using MoonCore.Models;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSystem;
using MoonlightServers.Daemon.ServerSystem.SubSystems;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerService : IHostedLifecycleService
{
    private readonly Dictionary<int, Server> Servers = new();

    private readonly RemoteService RemoteService;
    private readonly DockerClient DockerClient;
    private readonly IServiceProvider ServiceProvider;
    private readonly CancellationTokenSource TaskCancellation;
    private readonly ILogger<ServerService> Logger;
    private readonly IHubContext<ServerWebSocketHub> HubContext;

    public ServerService(
        RemoteService remoteService,
        IServiceProvider serviceProvider,
        DockerClient dockerClient,
        ILogger<ServerService> logger,
        IHubContext<ServerWebSocketHub> hubContext
    )
    {
        RemoteService = remoteService;
        ServiceProvider = serviceProvider;
        DockerClient = dockerClient;
        Logger = logger;
        HubContext = hubContext;

        TaskCancellation = new CancellationTokenSource();
    }

    public async Task Sync(int serverId)
    {
        if (Servers.TryGetValue(serverId, out var server))
        {
            var serverData = await RemoteService.GetServer(serverId);
            var configuration = serverData.ToServerConfiguration();

            server.Configuration = configuration;
        }
        else
            await Initialize(serverId);
    }

    public async Task Sync(int serverId, ServerConfiguration configuration)
    {
        if (Servers.TryGetValue(serverId, out var server))
            server.Configuration = configuration;
        else
            await Initialize(serverId);
    }

    public async Task InitializeAll()
    {
        var initialPage = await RemoteService.GetServers(0, 1);

        const int pageSize = 25;
        var pages = (initialPage.TotalItems == 0 ? 0 : (initialPage.TotalItems - 1) / pageSize) +
                    1; // The +1 is to handle the pages starting at 0

        // Create and fill a queue with pages to initialize
        var batchesLeft = new ConcurrentQueue<int>();

        for (var i = 0; i < pages; i++)
            batchesLeft.Enqueue(i);

        var tasksCount = pages > 5 ? 5 : pages;
        var tasks = new List<Task>();

        Logger.LogInformation(
            "Starting initialization for {count} server(s) with {tasksCount} worker(s)",
            initialPage.TotalItems,
            tasksCount
        );

        for (var i = 0; i < tasksCount; i++)
        {
            var id = i + 0;
            var task = Task.Run(() => BatchRunner(batchesLeft, id));

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Logger.LogInformation("Initialization completed");
    }

    private async Task BatchRunner(ConcurrentQueue<int> queue, int id)
    {
        while (!queue.IsEmpty)
        {
            if (!queue.TryDequeue(out var page))
                continue;

            await InitializeBatch(page, 25);

            Logger.LogDebug("Worker {id}: Finished initialization of page {page}", id, page);
        }

        Logger.LogDebug("Worker {id}: Finished", id);
    }

    private async Task InitializeBatch(int page, int pageSize)
    {
        var servers = await RemoteService.GetServers(page, pageSize);

        var configurations = servers.Items
            .Select(x => x.ToServerConfiguration())
            .ToArray();

        foreach (var configuration in configurations)
        {
            try
            {
                await Initialize(configuration);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "An unhandled error occured while initializing server {id}: {e}",
                    configuration.Id,
                    e
                );
            }
        }
    }

    public async Task Initialize(int serverId)
    {
        var serverData = await RemoteService.GetServer(serverId);
        var configuration = serverData.ToServerConfiguration();

        await Initialize(configuration);
    }

    public Server? Find(int serverId)
        => Servers.GetValueOrDefault(serverId);

    public async Task Initialize(ServerConfiguration configuration)
    {
        var serverScope = ServiceProvider.CreateScope();

        var server = new Server(configuration, serverScope, HubContext);

        Type[] subSystems =
        [
            typeof(ProvisionSubSystem),
            typeof(StorageSubSystem),
            typeof(DebugSubSystem),
            typeof(ShutdownSubSystem),
            typeof(ConsoleSubSystem),
            typeof(RestoreSubSystem),
            typeof(OnlineDetectionService),
            typeof(InstallationSubSystem)
        ];

        await server.Initialize(subSystems);

        Servers[configuration.Id] = server;
    }

    #region Docker Monitoring

    private async Task MonitorContainers()
    {
        Task.Run(async () =>
        {
            // Restart unless shutdown is requested
            while (!TaskCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    Logger.LogTrace("Starting to monitor events");

                    await DockerClient.System.MonitorEventsAsync(new(),
                        new Progress<Message>(async message =>
                        {
                            // Filter out unwanted events
                            if (message.Action != "die")
                                return;

                            // TODO: Implement a cached lookup using a shared dictionary by the sub system

                            var server = Servers.Values.FirstOrDefault(serverToCheck =>
                            {
                                var provisionSubSystem = serverToCheck.GetRequiredSubSystem<ProvisionSubSystem>();

                                if (provisionSubSystem.CurrentContainerId == message.ID)
                                    return true;

                                var installationSubSystem = serverToCheck.GetRequiredSubSystem<InstallationSubSystem>();

                                if (installationSubSystem.CurrentContainerId == message.ID)
                                    return true;

                                return false;
                            });

                            // If the container does not match any server we can ignore it
                            if (server == null)
                                return;

                            await server.StateMachine.FireAsync(ServerTrigger.Exited);
                        }), TaskCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    // Can be ignored
                }
                catch (Exception e)
                {
                    Logger.LogError("An unhandled error occured while monitoring events: {e}", e);
                }
            }
        });
    }

    #endregion

    #region Lifetime

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        await MonitorContainers();

        await InitializeAll();
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        foreach (var server in Servers.Values)
            await server.DisposeAsync();

        await TaskCancellation.CancelAsync();
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    #endregion

    /*
     *var existingContainers = await dockerClient.Containers.ListContainersAsync(new()
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
     *
     *
     *public async Task Delete(int serverId)
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
     *
     */
}