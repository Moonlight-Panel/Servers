using Moonlight.Client.Interfaces;
using Moonlight.Client.Models;

namespace MoonlightServers.Frontend.Implementations;

public class SidebarImplementation : ISidebarItemProvider
{
    public SidebarItem[] Get()
    {
        return
        [
            new SidebarItem()
            {
                Name = "Servers",
                Path = "/admin/servers",
                Icon = "icon-server",
                Group = "Admin",
                Priority = 4
            }
        ];
    }
}