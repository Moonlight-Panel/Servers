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

    private readonly ILogger<Server> Logger;
    
    private IAsyncDisposable? ProvisionExitSubscription;
    private IAsyncDisposable? InstallerExitSubscription;

    public Server(
        ILogger<Server> logger,
        IConsole console,
        IFileSystem fileSystem,
        IInstaller installer,
        IProvisioner provisioner,
        IRestorer restorer,
        IStatistics statistics,
        StateMachine<ServerState, ServerTrigger> stateMachine
    )
    {
        Logger = logger;
        Console = console;
        FileSystem = fileSystem;
        Installer = installer;
        Provisioner = provisioner;
        Restorer = restorer;
        Statistics = statistics;
        StateMachine = stateMachine;
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
        ProvisionExitSubscription = await Provisioner.OnExited.SubscribeAsync(async o =>
        {
            await StateMachine.FireAsync(ServerTrigger.Exited);
        });
        
        InstallerExitSubscription = await Installer.OnExited.SubscribeAsync(async o =>
        {
            await StateMachine.FireAsync(ServerTrigger.Exited);
        });
    }

    private void CreateStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(initialState, FiringMode.Queued);
        
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
        
    }

    public async ValueTask DisposeAsync()
    {
        if (ProvisionExitSubscription != null)
            await ProvisionExitSubscription.DisposeAsync();
        
        if(InstallerExitSubscription != null)
            await InstallerExitSubscription.DisposeAsync();
        
        await Console.DisposeAsync();
        await FileSystem.DisposeAsync();
        await Installer.DisposeAsync();
        await Provisioner.DisposeAsync();
        await Restorer.DisposeAsync();
        await Statistics.DisposeAsync();
    }
}