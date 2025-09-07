using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem;

public partial class Server : IAsyncDisposable
{
    public int Identifier => InnerContext.Identifier;
    public ServerContext Context => InnerContext;

    public IConsole Console { get; }
    public IFileSystem RuntimeFileSystem { get; }
    public IFileSystem InstallationFileSystem { get; }
    public IInstallation Installation { get; }
    public IOnlineDetector OnlineDetector { get; }
    public IReporter Reporter { get; }
    public IRestorer Restorer { get; }
    public IRuntime Runtime { get; }
    public IStatistics Statistics { get; }
    public StateMachine<ServerState, ServerTrigger> StateMachine { get; private set; }

    private readonly IServerStateHandler[] Handlers;

    private readonly IServerComponent[] AllComponents;
    private readonly ServerContext InnerContext;
    private readonly ILogger Logger;

    public Server(
        ILogger logger,
        ServerContext context,
        IConsole console,
        IFileSystem runtimeFileSystem,
        IFileSystem installationFileSystem,
        IInstallation installation,
        IOnlineDetector onlineDetector,
        IReporter reporter,
        IRestorer restorer,
        IRuntime runtime,
        IStatistics statistics,
        IServerStateHandler[] handlers
    )
    {
        Logger = logger;
        InnerContext = context;
        Console = console;
        RuntimeFileSystem = runtimeFileSystem;
        InstallationFileSystem = installationFileSystem;
        Installation = installation;
        OnlineDetector = onlineDetector;
        Reporter = reporter;
        Restorer = restorer;
        Runtime = runtime;
        Statistics = statistics;

        AllComponents =
        [
            Console, RuntimeFileSystem, InstallationFileSystem, Installation, OnlineDetector, Reporter, Restorer,
            Runtime, Statistics
        ];

        Handlers = handlers;
    }

    private void ConfigureStateMachine(ServerState initialState)
    {
        StateMachine = new StateMachine<ServerState, ServerTrigger>(
            initialState, FiringMode.Queued
        );

        StateMachine.Configure(ServerState.Offline)
            .Permit(ServerTrigger.Start, ServerState.Starting)
            .Permit(ServerTrigger.Install, ServerState.Installing)
            .PermitReentry(ServerTrigger.Fail);

        StateMachine.Configure(ServerState.Starting)
            .Permit(ServerTrigger.DetectOnline, ServerState.Online)
            .Permit(ServerTrigger.Fail, ServerState.Offline)
            .Permit(ServerTrigger.Exited, ServerState.Offline)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping);

        StateMachine.Configure(ServerState.Online)
            .Permit(ServerTrigger.Stop, ServerState.Stopping)
            .Permit(ServerTrigger.Kill, ServerState.Stopping)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Stopping)
            .PermitReentry(ServerTrigger.Fail)
            .PermitReentry(ServerTrigger.Kill)
            .Permit(ServerTrigger.Exited, ServerState.Offline);

        StateMachine.Configure(ServerState.Installing)
            .Permit(ServerTrigger.Fail, ServerState.Offline) // TODO: Add kill
            .Permit(ServerTrigger.Exited, ServerState.Offline);
    }

    private void ConfigureStateMachineEvents()
    {
        // Configure the calling of the handlers
        StateMachine.OnTransitionedAsync(async transition =>
        {
            var hasFailed = false;

            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.ExecuteAsync(transition);
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        e,
                        "Handler {name} has thrown an unexpected exception",
                        handler.GetType().FullName
                    );

                    hasFailed = true;
                    break;
                }
            }
            
            if(!hasFailed)
                return; // Everything went fine, we can exit now
            
            // Something has failed, lets check if we can handle the error
            // via a fail trigger
            
            if(!StateMachine.CanFire(ServerTrigger.Fail))
                return;
            
            // Trigger the fail so the server gets a chance to handle the error softly
            await StateMachine.FireAsync(ServerTrigger.Fail);
        });
    }

    public async Task InitializeAsync()
    {
        foreach (var component in AllComponents)
            await component.InitializeAsync();

        var restoredState = ServerState.Offline;

        ConfigureStateMachine(restoredState);
        ConfigureStateMachineEvents();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var handler in Handlers)
            await handler.DisposeAsync();
        
        foreach (var component in AllComponents)
            await component.DisposeAsync();
    }
}