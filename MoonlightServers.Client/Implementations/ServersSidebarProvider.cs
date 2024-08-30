using Moonlight.Client.App.Interfaces;
using Moonlight.Client.App.Models;

namespace MoonlightServers.Client.Implementations;

public class ServersSidebarProvider : ISidebarItemProvider
{
    public SidebarItem[] GetItems()
    {
        return [new ()
        {
            Name = "Servers",
            Target = "/servers",
            Icon = "bi bi-hdd-rack",
            Priority = 5
        },
        new()
        {
            Name = "Servers",
            Target = "/admin/servers",
            Priority = 5,
            Icon = "bi bi-hdd-rack",
            Group = "Admin",
            Permission = "admin.servers.get"
        }];
    }
}