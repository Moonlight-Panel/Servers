using MoonlightServers.Daemon.ServerSystem;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IRestorer : IServerComponent
{
    public Task<ServerState> Restore();
}