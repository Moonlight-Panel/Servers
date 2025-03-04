using MoonlightServers.Frontend.Interfaces;
using MoonlightServers.Frontend.Models;
using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;
using MoonlightServers.Shared.Http.Responses.Users.Servers;

namespace MoonlightServers.Frontend.Implementations;

public class DefaultServerTabProvider : IServerTabProvider
{
    public Task<ServerTab[]> GetTabs(ServerDetailResponse server)
    {
        ServerTab[] tabs =
        [
            ServerTab.CreateFromComponent<ConsoleTab>("Console", "console", 0),
            ServerTab.CreateFromComponent<FilesTab>("Files", "files", 1),
            ServerTab.CreateFromComponent<VariablesTab>("Variables", "variables", 2),
            ServerTab.CreateFromComponent<SettingsTab>("Settings", "settings", 10),
        ];

        return Task.FromResult(tabs);
    }
}