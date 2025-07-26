using System.Reactive.Subjects;
using MoonlightServers.Daemon.ServerSystem;
using Stateless;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public class Server : IAsyncDisposable
{
    public IConsole Console { get; private set; }
    public IFileSystem FileSystem { get; private set; }
    public IInstaller Installer { get; private set; }
    public IProvisioner Provisioner { get; private set; }
    public IRestorer Restorer { get; private set; }
    public IStatistics Statistics { get; private set; }
    public StateMachine<ServerState, ServerTrigger> StateMachine { get; private set; }
    public ServerContext Context { get; private set; }
    public IObservable<ServerState> OnState => OnStateSubject;

    private readonly Subject<ServerState> OnStateSubject = new();
    private readonly ILogger<Server> Logger;
    
    private IDisposable? ProvisionExitSubscription;
    private IDisposable? InstallerExitSubscription;

    public Server(
        ILogger<Server> logger,
        IConsole console,
        IFileSystem fileSystem,
        IInstaller installer,
        IProvisioner provisioner,
        IRestorer restorer,
        IStatistics statistics,
        ServerContext context
    )
    {
        Logger = logger;
        Console = console;
        FileSystem = fileSystem;
        Installer = installer;
        Provisioner = provisioner;
        Restorer = restorer;
        Statistics = statistics;
        Context = context;
    }

    public async Task Initialize()
    {
        Logger.LogDebug("Initializing server components");

        IServerComponent[] components = [Console, Restorer, FileSystem, Installer, Provisioner, Statistics];

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

        // Setup event handling
        ProvisionExitSubscription = Provisioner.OnExited.Subscribe(o =>
        {
            StateMachine.Fire(ServerTrigger.Exited);
        });

        InstallerExitSubscription = Installer.OnExited.Subscribe(o =>
        {
            StateMachine.Fire(ServerTrigger.Exited);
        });
    }

    private void CreateStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(initialState, FiringMode.Queued);
        
        StateMachine.OnTransitioned(transition => OnStateSubject.OnNext(transition.Destination));

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
            .Permit(ServerTrigger.FailSafe, ServerState.Offline)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        // Handle transitions

        StateMachine.Configure(ServerState.Starting)
            .OnEntryAsync(HandleStart);

        StateMachine.Configure(ServerState.Stopping)
            .OnEntryFromAsync(ServerTrigger.Stop, HandleStop);
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
            // TODO: Implement

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

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (ProvisionExitSubscription != null)
            ProvisionExitSubscription.Dispose();

        if (InstallerExitSubscription != null)
            InstallerExitSubscription.Dispose();

        await Console.DisposeAsync();
        await FileSystem.DisposeAsync();
        await Installer.DisposeAsync();
        await Provisioner.DisposeAsync();
        await Restorer.DisposeAsync();
        await Statistics.DisposeAsync();
    }
}