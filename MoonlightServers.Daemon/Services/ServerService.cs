using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Attributes;
using MoonCore.Models;
using MoonlightServers.Daemon.Extensions.ServerExtensions;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.DaemonShared.Enums;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerService
{
    private readonly List<Server> Servers = new();
    private readonly ILogger<ServerService> Logger;
    private readonly RemoteService RemoteService;
    private readonly IServiceProvider ServiceProvider;
    private bool IsInitialized = false;
    private CancellationTokenSource Cancellation = new();

    public ServerService(RemoteService remoteService, ILogger<ServerService> logger, IServiceProvider serviceProvider)
    {
        RemoteService = remoteService;
        Logger = logger;
        ServiceProvider = serviceProvider;
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
        var apiClient = await RemoteService.CreateHttpClient();

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

        foreach (var configuration in configurations)
            await InitializeServer(configuration);
        
        // Attach to docker events
        await AttachToDockerEvents();
    }

    public async Task Stop()
    {
        Server[] servers;

        lock (Servers)
            servers = Servers.ToArray();

        Logger.LogTrace("Canceling server sub tasks");
        
        foreach (var server in servers)
            await server.Cancellation.CancelAsync();
        
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
                            if(message.Action != "die")
                                return;

                            Server? server;

                            lock (Servers)
                                server = Servers.FirstOrDefault(x => x.ContainerId == message.ID);

                            // TODO: Maybe implement a lookup for containers which id isn't set in the cache
                            
                            if(server == null)
                                return;

                            await server.StateMachine.TransitionTo(ServerState.Offline);
                        }), Cancellation.Token);
                }
                catch(TaskCanceledException){} // Can be ignored
                catch (Exception e)
                {
                    Logger.LogError("An unhandled error occured while attaching to docker events: {e}", e);
                }
            }
        });
    }
    
    private async Task InitializeServer(ServerConfiguration configuration)
    {
        Logger.LogTrace("Initializing server '{id}'", configuration.Id);

        var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
        
        var server = new Server()
        {
            Configuration = configuration,
            StateMachine = new(ServerState.Offline),
            ServiceProvider = ServiceProvider,
            Logger = loggerFactory.CreateLogger($"Server {configuration.Id}"),
            Console = new(),
            Cancellation = new()
        };

        server.StateMachine.OnError += (state, exception) =>
        {
            server.Logger.LogError("Encountered an unhandled error while transitioning to {state}: {e}",
                state,
                exception
            );
        };

        server.StateMachine.OnTransitioned += state =>
        {
            server.Logger.LogInformation("State: {state}", state);
            return Task.CompletedTask;
        };

        server.StateMachine.AddTransition(ServerState.Offline, ServerState.Starting, ServerState.Offline, async () =>
            await server.StateMachineHandler_Start()
        );
        
        server.StateMachine.AddTransition(ServerState.Starting, ServerState.Offline);
        server.StateMachine.AddTransition(ServerState.Online, ServerState.Offline);
        server.StateMachine.AddTransition(ServerState.Stopping, ServerState.Offline);
        server.StateMachine.AddTransition(ServerState.Installing, ServerState.Offline);

        lock (Servers)
            Servers.Add(server);
    }

    public Server? GetServer(int id)
    {
        lock (Servers)
            return Servers.FirstOrDefault(x => x.Configuration.Id == id);
    }
}