using Moonlight.Client.Interfaces;
using Moonlight.Client.Models;

namespace MoonlightServers.Frontend.Implementations;

public class SidebarImplementation : ISidebarItemProvider
{
    public void ModifySidebar(List<SidebarItem> items)
    {
        items.AddRange(
            [
                new SidebarItem()
                {
                    Name = "Servers",
                    Path = "/servers",
                    Icon = "icon-server",
                    Priority = 4
                },
                new SidebarItem()
                {
                    Name = "Servers",
                    Path = "/admin/servers",
                    Icon = "icon-server",
                    Group = "Admin",
                    Priority = 4
                }
            ]
        );
    }
}