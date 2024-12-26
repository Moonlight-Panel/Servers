using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Models.Cache;
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
    }

    private async Task InitializeServer(ServerConfiguration configuration)
    {
        Logger.LogInformation("Initializing server '{id}'", configuration.Id);

        var server = new Server()
        {
            Configuration = configuration,
            StateMachine = new(ServerState.Offline)
        };

        server.StateMachine.OnError += (state, exception) =>
        {
            Logger.LogError("Server {id} encountered an unhandled error while transitioning to {state}: {e}",
                server.Configuration.Id,
                state,
                exception
            );
        };

        server.StateMachine.AddTransition(ServerState.Offline, ServerState.Starting, ServerState.Offline, async () =>
            await ServerActionHelper.Start(server, ServiceProvider)
        );

        lock (Servers)
            Servers.Add(server);
    }

    public Server? GetServer(int id)
    {
        lock (Servers)
            return Servers.FirstOrDefault(x => x.Configuration.Id == id);
    }
}