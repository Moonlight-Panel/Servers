using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Models;

public class ServerShareContent
{
    public Dictionary<string, ServerPermissionLevel> Permissions { get; set; } = new();
}