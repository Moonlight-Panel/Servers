using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.Frontend.Models;

public record ServerTab
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public string PermissionId { get; set; }
    public ServerPermissionLevel PermissionLevel { get; set; }
    public int Priority { get; private set; }
    public Type ComponentType { get; private set; }

    public static ServerTab CreateFromComponent<T>(
        string name,
        string path,
        int priority,
        string permissionId = "", ServerPermissionLevel permissionLevel = ServerPermissionLevel.None) where T : BaseServerTab
    {
        return new()
        {
            Name = name,
            Path = path,
            Priority = priority,
            ComponentType = typeof(T),
            PermissionLevel = permissionLevel,
            PermissionId = permissionId
        };
    }
}