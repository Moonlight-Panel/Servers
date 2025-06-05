using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Models;

public class ServerSharePermission
{
    public string Name { get; set; }
    public ServerPermissionType Type { get; set; }
}