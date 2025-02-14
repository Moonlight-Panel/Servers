using Stateless;

namespace MoonlightServers.Daemon.Extensions;

public static class StateConfigurationExtensions
{
    public static StateMachine<TState, TTrigger>.StateConfiguration OnExitFrom<TState, TTrigger>(
        this StateMachine<TState, TTrigger>.StateConfiguration configuration, TTrigger trigger, Action entryAction
    )
    {
        configuration.OnExit(transition =>
        {
            if(!transition.Trigger!.Equals(trigger))
                return;
            
            entryAction.Invoke();
        });

        return configuration;
    }
    
    public static StateMachine<TState, TTrigger>.StateConfiguration OnExitFrom<TState, TTrigger>(
        this StateMachine<TState, TTrigger>.StateConfiguration configuration, TTrigger trigger, Action<StateMachine<TState, TTrigger>.Transition> entryAction
    )
    {
        configuration.OnExit(transition =>
        {
            if(!transition.Trigger!.Equals(trigger))
                return;
            
            entryAction.Invoke(transition);
        });

        return configuration;
    }
    
    public static StateMachine<TState, TTrigger>.StateConfiguration OnExitFromAsync<TState, TTrigger>(
        this StateMachine<TState, TTrigger>.StateConfiguration configuration, TTrigger trigger, Func<Task> entryAction
    )
    {
        configuration.OnExitAsync(transition =>
        {
            if(!transition.Trigger!.Equals(trigger))
                return Task.CompletedTask;
            
            return entryAction.Invoke();
        });

        return configuration;
    }
    
    public static StateMachine<TState, TTrigger>.StateConfiguration OnExitFromAsync<TState, TTrigger>(
        this StateMachine<TState, TTrigger>.StateConfiguration configuration, TTrigger trigger, Func<StateMachine<TState, TTrigger>.Transition, Task> entryAction
    )
    {
        configuration.OnExitAsync(transition =>
        {
            if(!transition.Trigger!.Equals(trigger))
                return Task.CompletedTask;
            
            return entryAction.Invoke(transition);
        });

        return configuration;
    }
}