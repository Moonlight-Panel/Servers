using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

public class ServerStatusResponse
{
    public ServerState State { get; set; }
}