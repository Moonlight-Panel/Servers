using Microsoft.AspNetCore.SignalR;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models.Cache;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem;

public class Server : IAsyncDisposable
{
    public ServerConfiguration Configuration { get; set; }
    public CancellationToken TaskCancellation => TaskCancellationSource.Token;
    internal StateMachine<ServerState, ServerTrigger> StateMachine { get; private set; }
    private CancellationTokenSource TaskCancellationSource;

    private Dictionary<Type, ServerSubSystem> SubSystems = new();
    private ServerState InternalState = ServerState.Offline;

    private readonly IHubContext<ServerWebSocketHub> HubContext;
    private readonly IServiceScope ServiceScope;
    private readonly ILoggerFactory LoggerFactory;
    private readonly ILogger Logger;
    
    public Server(
        ServerConfiguration configuration,
        IServiceScope serviceScope,
        IHubContext<ServerWebSocketHub> hubContext
    )
    {
        Configuration = configuration;
        ServiceScope = serviceScope;
        HubContext = hubContext;

        TaskCancellationSource = new CancellationTokenSource();

        LoggerFactory = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        Logger = LoggerFactory.CreateLogger($"Server {Configuration.Id}");

        StateMachine = new StateMachine<ServerState, ServerTrigger>(
            () => InternalState,
            state => InternalState = state,
            FiringMode.Queued
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
            .Permit(ServerTrigger.FailSafe, ServerState.Offline)
            .Permit(ServerTrigger.Exited, ServerState.Offline);
        
        StateMachine.Configure(ServerState.Offline)
            .OnEntryAsync(async () =>
            {
                // Configure task reset when server goes offline
                
                if (!TaskCancellationSource.IsCancellationRequested)
                    await TaskCancellationSource.CancelAsync();
            })
            .OnExit(() =>
            {
                // Activate tasks when the server goes online
                // If we don't separate the disabling and enabling
                // of the tasks and would do both it in just the offline handler
                // we would have edge cases where reconnect loops would already have the new task activated
                // while they are supposed to shut down. I tested the handling of the state machine,
                // and it executes on exit before the other listeners from the other sub systems
                TaskCancellationSource = new();
            });

        // Setup websocket notify for state changes
        StateMachine.OnTransitionedAsync(async transition =>
        {
            await HubContext.Clients
                .Group(Configuration.Id.ToString())
                .SendAsync("StateChanged", transition.Destination.ToString());
        });
    }

    public async Task Initialize(Type[] subSystemTypes)
    {
        foreach (var type in subSystemTypes)
        {
            var logger = LoggerFactory.CreateLogger($"Server {Configuration.Id} - {type.Name}");

            var subSystem = ActivatorUtilities.CreateInstance(
                ServiceScope.ServiceProvider,
                type,
                this,
                logger
            ) as ServerSubSystem;

            if (subSystem == null)
            {
                Logger.LogError("Unable to construct server sub system: {name}", type.Name);
                continue;
            }

            SubSystems.Add(type, subSystem);
        }

        foreach (var type in SubSystems.Keys)
        { 
            try
            {
                await SubSystems[type].Initialize();
            }
            catch (Exception e)
            {
                Logger.LogError("An unhandled error occured while initializing sub system {name}: {e}", type.Name, e);
            }
        }
    }

    public async Task Trigger(ServerTrigger trigger)
    {
        if (!StateMachine.CanFire(trigger))
            throw new HttpApiException($"The trigger {trigger} is not supported during the state {StateMachine.State}", 400);

        await StateMachine.FireAsync(trigger);
    }

    public async Task Delete()
    {
        foreach (var subSystem in SubSystems.Values)
            await subSystem.Delete();
    }

    // This method completely bypasses the state machine.
    // Using this method without any checks will lead to
    // broken server states. Use with caution
    public void OverrideState(ServerState state)
    {
        InternalState = state;
    }

    public T? GetSubSystem<T>() where T : ServerSubSystem
    {
        var type = typeof(T);
        var subSystem = SubSystems.GetValueOrDefault(type);

        if (subSystem == null)
            return null;

        return subSystem as T;
    }

    public T GetRequiredSubSystem<T>() where T : ServerSubSystem
    {
        var subSystem = GetSubSystem<T>();

        if (subSystem == null)
            throw new AggregateException("Unable to resolve requested sub system");

        return subSystem;
    }

    public async ValueTask DisposeAsync()
    {
        if (!TaskCancellationSource.IsCancellationRequested)
            await TaskCancellationSource.CancelAsync();

        foreach (var subSystem in SubSystems.Values)
            await subSystem.DisposeAsync();
        
        ServiceScope.Dispose();
    }
}