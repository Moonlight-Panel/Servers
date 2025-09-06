using MoonlightServers.Daemon.ServerSystem.Enums;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IServerStateHandler : IAsyncDisposable
{
    public Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition);
}