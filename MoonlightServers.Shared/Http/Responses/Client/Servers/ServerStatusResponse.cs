using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Http.Responses.Client.Servers;

public class ServerStatusResponse
{
    public ServerState State { get; set; }
}