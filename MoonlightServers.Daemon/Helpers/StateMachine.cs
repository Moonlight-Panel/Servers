namespace MoonlightServers.Daemon.Helpers;

public class StateMachine<T> where T : struct, Enum
{
    private readonly List<StateMachineTransition> Transitions = new();
    private readonly object Lock = new();

    public T CurrentState { get; private set; }
    public event Func<T, Task> OnTransitioned;
    public event Action<T, Exception> OnError;

    public StateMachine(T initialState)
    {
        CurrentState = initialState;
    }

    public void AddTransition(T from, T to, T? onError, Func<Task> fun)
    {
        Transitions.Add(new()
        {
            From = from,
            To = to,
            OnError = onError,
            OnTransitioning = fun
        });
    }

    public void AddTransition(T from, T to, Func<Task> fun) => AddTransition(from, to, null, fun);

    public async Task TransitionTo(T to)
    {
        lock (Lock)
        {
            var transition = Transitions.FirstOrDefault(x =>
                x.From.Equals(CurrentState) &&
                x.To.Equals(to)
            );

            if (transition == null)
                throw new InvalidOperationException("Unable to transition to the request state: No transition found");

            try
            {
                transition.OnTransitioning.Invoke().Wait();
            }
            catch (Exception e)
            {
                if(OnError != null)
                    OnError.Invoke(to, e);

                if (transition.OnError.HasValue)
                    CurrentState = transition.OnError.Value;
                else
                    throw new AggregateException("An error occured while transitioning to a state", e);
            }
        }

        if(OnTransitioned != null)
            await OnTransitioned.Invoke(CurrentState);
    }

    public class StateMachineTransition
    {
        public T From { get; set; }
        public T To { get; set; }
        public T? OnError { get; set; }
        public Func<Task> OnTransitioning { get; set; }
    }
}