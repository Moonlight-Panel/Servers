using MoonlightServers.Daemon.Models.Cache;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem;

public abstract class ServerSubSystem : IAsyncDisposable
{
    protected Server Server { get; private set; }
    protected ServerConfiguration Configuration => Server.Configuration;
    protected ILogger Logger { get; private set; }
    protected StateMachine<ServerState, ServerTrigger> StateMachine => Server.StateMachine;

    protected ServerSubSystem(Server server, ILogger logger)
    {
        Server = server;
        Logger = logger;
    }

    public virtual Task Initialize()
        => Task.CompletedTask;

    public virtual Task Delete()
        => Task.CompletedTask;

    public virtual ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}