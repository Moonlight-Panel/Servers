using Microsoft.AspNetCore.SignalR;
using MoonCore.Observability;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.ServerSystem;
using MoonlightServers.Daemon.Services;
using Stateless;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public class Server : IAsyncDisposable
{
    public IConsole Console { get; }
    public IFileSystem FileSystem { get; }
    public IInstaller Installer { get; }
    public IProvisioner Provisioner { get; }
    public IRestorer Restorer { get; }
    public IStatistics Statistics { get; }
    public IOnlineDetection OnlineDetection { get; }
    public StateMachine<ServerState, ServerTrigger> StateMachine { get; private set; }
    public ServerContext Context { get; }
    public IAsyncObservable<ServerState> OnState => OnStateSubject;

    private readonly EventSubject<ServerState> OnStateSubject = new();
    private readonly ILogger<Server> Logger;
    private readonly RemoteService RemoteService;
    private readonly ServerConfigurationMapper Mapper;
    private readonly IHubContext<ServerWebSocketHub> HubContext;

    private IAsyncDisposable? ProvisionExitSubscription;
    private IAsyncDisposable? InstallerExitSubscription;
    private IAsyncDisposable? ConsoleSubscription;

    public Server(
        ILogger<Server> logger,
        IConsole console,
        IFileSystem fileSystem,
        IInstaller installer,
        IProvisioner provisioner,
        IRestorer restorer,
        IStatistics statistics,
        IOnlineDetection onlineDetection,
        ServerContext context,
        RemoteService remoteService,
        ServerConfigurationMapper mapper,
        IHubContext<ServerWebSocketHub> hubContext)
    {
        Logger = logger;
        Console = console;
        FileSystem = fileSystem;
        Installer = installer;
        Provisioner = provisioner;
        Restorer = restorer;
        Statistics = statistics;
        Context = context;
        RemoteService = remoteService;
        Mapper = mapper;
        HubContext = hubContext;
        OnlineDetection = onlineDetection;
    }

    public async Task Initialize()
    {
        Logger.LogDebug("Initializing server components");

        IServerComponent[] components = [Console, Restorer, FileSystem, Installer, Provisioner, Statistics, OnlineDetection];

        foreach (var serverComponent in components)
        {
            try
            {
                await serverComponent.Initialize();
            }
            catch (Exception e)
            {
                Logger.LogError(
                    e,
                    "Error initializing server component: {type}",
                    serverComponent.GetType().Name.GetType().FullName
                );

                throw;
            }
        }

        Logger.LogDebug("Restoring server");
        var restoredState = await Restorer.Restore();

        if (restoredState == ServerState.Offline)
            Logger.LogDebug("Restorer didnt find anything to restore. State is offline");
        else
            Logger.LogDebug("Restored server to state: {state}", restoredState);

        CreateStateMachine(restoredState);

        await SetupHubEvents();

        // Setup event handling
        ProvisionExitSubscription = await Provisioner.OnExited.SubscribeEventAsync(async _ =>
            await StateMachine.FireAsync(ServerTrigger.Exited)
        );

        InstallerExitSubscription = await Installer.OnExited.SubscribeEventAsync(async _ =>
            await StateMachine.FireAsync(ServerTrigger.Exited)
        );
    }

    public async Task Sync()
    {
        IServerComponent[] components = [Console, Restorer, FileSystem, Installer, Provisioner, Statistics, OnlineDetection];

        foreach (var component in components)
            await component.Sync();
    }

    private void CreateStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(initialState, FiringMode.Queued);

        StateMachine.OnTransitionedAsync(async transition
            => await OnStateSubject.OnNextAsync(transition.Destination)
        );

        // Configure basic state machine flow

        StateMachine.Configure(ServerState.Offline)
            .Permit(ServerTrigger.Start, ServerState.Starting)
            .Permit(ServerTrigger.Install, ServerState.Installing)
            .PermitReentry(ServerTrigger.FailSafe);

        StateMachine.Configure(ServerState.Starting)
            .Permit(ServerTrigger.OnlineDetected, ServerState.Online)
            .Permit(ServerTrigger.FailSafe, ServerState.Offline)
            .Permit(ServerTrigger.Exited, ServerState.Offline)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping);

        StateMachine.Configure(ServerState.Online)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Stopping)
            .PermitReentry(ServerTrigger.FailSafe)
            .PermitReentry(ServerTrigger.Kill)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Installing)
            .Permit(ServerTrigger.FailSafe, ServerState.Offline) // TODO: Add kill
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        // Handle transitions

        StateMachine.Configure(ServerState.Starting)
            .OnEntryAsync(HandleStart)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);

        StateMachine.Configure(ServerState.Installing)
            .OnEntryAsync(HandleInstall)
            .OnExitFromAsync(ServerTrigger.Exited, HandleInstallExit);

        StateMachine.Configure(ServerState.Online)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);

        StateMachine.Configure(ServerState.Stopping)
            .OnEntryFromAsync(ServerTrigger.Stop, HandleStop)
            .OnEntryFromAsync(ServerTrigger.Kill, HandleKill)
            .OnExitFromAsync(ServerTrigger.Exited, HandleRuntimeExit);
    }

    private async Task SetupHubEvents()
    {
        var groupName = Context.Configuration.Id.ToString();
        
        ConsoleSubscription = await Console.OnOutput.SubscribeAsync(async line =>
        {
            await HubContext.Clients.Group(groupName).SendAsync(
                "ConsoleOutput",
                line
            );
        });
        
        StateMachine.OnTransitionedAsync(async transition =>
        {
            await HubContext.Clients.Group(groupName).SendAsync(
                "StateChanged",
                transition.Destination.ToString()
            );
        });
    }

    #region State machine handlers

    private async Task HandleStart()
    {
        try
        {
            // Plan for starting the server:
            // 1. Fetch latest configuration from panel (maybe: and perform sync)
            // 2. Ensure that the file system exists
            // 3. Mount the file system
            // 4. Provision the container
            // 5. Attach console to container
            // 6. Start the container

            // 1. Fetch latest configuration from panel
            Logger.LogDebug("Fetching latest server configuration");
            await Console.WriteToMoonlight("Fetching latest server configuration");

            var serverDataResponse = await RemoteService.GetServer(Context.Configuration.Id);
            Context.Configuration = Mapper.FromServerDataResponse(serverDataResponse);

            // 2. Ensure that the file system exists
            if (!FileSystem.Exists)
            {
                await Console.WriteToMoonlight("Creating storage");
                await FileSystem.Create();
            }

            // 3. Mount the file system
            if (!FileSystem.IsMounted)
            {
                await Console.WriteToMoonlight("Mounting storage");
                await FileSystem.Mount();
            }

            // 4. Provision the container
            await Console.WriteToMoonlight("Provisioning runtime");
            await Provisioner.Provision();

            // 5. Attach console to container
            await Console.AttachToRuntime();

            // 6. Start the container
            await Provisioner.Start();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while starting the server");
        }
    }

    private async Task HandleStop()
    {
        await Provisioner.Stop();
    }

    private async Task HandleKill()
    {
        await Provisioner.Kill();
    }

    private async Task HandleRuntimeExit()
    {
        Logger.LogDebug("Detected runtime exit");
        
        Logger.LogDebug("Detaching from console");
        await Console.Detach();
        
        Logger.LogDebug("Deprovisioning");
        await Console.WriteToMoonlight("Deprovisioning");
        await Provisioner.Deprovision();
    }

    private async Task HandleInstall()
    {
        // Plan:
        // 1. Fetch the latest installation data
        // 2. Setup installation environment
        // 3. Attach console to installation
        // 4. Start the installation

        Logger.LogDebug("Installing");

        Logger.LogDebug("Setting up");
        await Console.WriteToMoonlight("Setting up installation");

        // 1. Fetch the latest installation data
        Logger.LogDebug("Fetching installation data");
        await Console.WriteToMoonlight("Fetching installation data");

        Context.InstallConfiguration = await RemoteService.GetServerInstallation(Context.Configuration.Id);

        // 2. Setup installation environment
        await Installer.Setup();

        // 3. Attach console to installation
        await Console.AttachToInstallation();

        // 4. Start the installation
        await Installer.Start();
    }

    private async Task HandleInstallExit()
    {
        Logger.LogDebug("Detected install exit");
        
        Logger.LogDebug("Detaching from console");
        await Console.Detach();
        
        Logger.LogDebug("Cleaning up");
        await Console.WriteToMoonlight("Cleaning up");
        await Installer.Cleanup();

        await Console.WriteToMoonlight("Installation completed");
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (ProvisionExitSubscription != null)
            await ProvisionExitSubscription.DisposeAsync();

        if (InstallerExitSubscription != null)
            await InstallerExitSubscription.DisposeAsync();

        if (ConsoleSubscription != null)
            await ConsoleSubscription.DisposeAsync();

        await Console.DisposeAsync();
        await FileSystem.DisposeAsync();
        await Installer.DisposeAsync();
        await Provisioner.DisposeAsync();
        await Restorer.DisposeAsync();
        await Statistics.DisposeAsync();
    }
}