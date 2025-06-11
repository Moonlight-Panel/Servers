using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Models;

public record ServerSharePermission
{
    public string Name { get; set; }
    public ServerPermissionType Type { get; set; }
}