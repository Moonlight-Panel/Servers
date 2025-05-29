namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class DebugSubSystem : ServerSubSystem
{
    public DebugSubSystem(Server server, ILogger logger) : base(server, logger)
    {
        
    }

    public override Task Initialize()
    {
        StateMachine.OnTransitioned(transition =>
        {
            Logger.LogTrace("State: {state} via {trigger}", transition.Destination, transition.Trigger);
        });
        
        return Task.CompletedTask;
    }
}