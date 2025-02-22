using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;

namespace MoonlightServers.Frontend.Models;

public class ServerTab
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public int Priority { get; set; }
    public Type ComponentType { get; private set; }

    public static ServerTab CreateFromComponent<T>(string name, string path, int priority) where T : BaseServerTab
    {
        return new()
        {
            Name = name,
            Path = path,
            Priority = priority,
            ComponentType = typeof(T)
        };
    }
}