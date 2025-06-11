using MoonlightServers.Frontend.Interfaces;
using MoonlightServers.Frontend.Models;
using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;
using MoonlightServers.Shared.Http.Responses.Client.Servers;

namespace MoonlightServers.Frontend.Implementations;

public class DefaultServerTabProvider : IServerTabProvider
{
    public Task<ServerTab[]> GetTabs(ServerDetailResponse server)
    {
        ServerTab[] tabs =
        [
            ServerTab.CreateFromComponent<ConsoleTab>("Console", "console", 0, permission => permission.Name == "console"),
            ServerTab.CreateFromComponent<FilesTab>("Files", "files", 1, permission => permission.Name == "files"),
            ServerTab.CreateFromComponent<SharesTab>("Shares", "shares", 2, permission => permission.Name == "shares"),
            ServerTab.CreateFromComponent<VariablesTab>("Variables", "variables", 9, permission => permission.Name == "variables"),
            ServerTab.CreateFromComponent<SettingsTab>("Settings", "settings", 10, permission => permission.Name == "settings"),
        ];

        return Task.FromResult(tabs);
    }
}