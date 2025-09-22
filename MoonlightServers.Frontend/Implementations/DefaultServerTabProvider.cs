using MoonlightServers.Frontend.Interfaces;
using MoonlightServers.Frontend.Models;
using MoonlightServers.Frontend.UI.Components.Servers.ServerTabs;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Responses.Client.Servers;

namespace MoonlightServers.Frontend.Implementations;

public class DefaultServerTabProvider : IServerTabProvider
{
    public Task<ServerTab[]> GetTabsAsync(ServerDetailResponse server)
    {
        ServerTab[] tabs =
        [
            ServerTab.CreateFromComponent<ConsoleTab>("Console", "console", 0, ServerPermissionConstants.Console, ServerPermissionLevel.Read),
            ServerTab.CreateFromComponent<FilesTab>("Files", "files", 1, ServerPermissionConstants.Files, ServerPermissionLevel.Read),
            ServerTab.CreateFromComponent<SharesTab>("Shares", "shares", 2, ServerPermissionConstants.Shares, ServerPermissionLevel.ReadWrite),
            ServerTab.CreateFromComponent<VariablesTab>("Variables", "variables", 9, ServerPermissionConstants.Variables, ServerPermissionLevel.Read),
            ServerTab.CreateFromComponent<SettingsTab>("Settings", "settings", 10, ServerPermissionConstants.Settings, ServerPermissionLevel.Read),
        ];

        return Task.FromResult(tabs);
    }
}