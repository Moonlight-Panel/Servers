using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.Frontend.Models;

public record ServerTab
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public Func<ServerSharePermission, bool>? PermissionFilter { get; private set; }
    public int Priority { get; private set; }
    public Type ComponentType { get; private set; }

    public static ServerTab CreateFromComponent<T>(
        string name,
        string path,
        int priority,
        Func<ServerSharePermission, bool>? filter = null) where T : BaseServerTab
    {
        return new()
        {
            Name = name,
            Path = path,
            Priority = priority,
            ComponentType = typeof(T),
            PermissionFilter = filter
        };
    }
}