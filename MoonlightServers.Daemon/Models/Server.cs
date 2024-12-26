using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models.Cache;

namespace MoonlightServers.Daemon.Models;

public class Server
{
    public ServerState State => StateMachine.CurrentState;
    public StateMachine<ServerState> StateMachine { get; set; }
    public ServerConfiguration Configuration { get; set; }
    public string? ContainerId { get; set; }
}