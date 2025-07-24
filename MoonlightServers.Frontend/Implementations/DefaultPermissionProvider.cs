using MoonlightServers.Frontend.Interfaces;
using MoonlightServers.Frontend.Models;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Http.Responses.Client.Servers;

namespace MoonlightServers.Frontend.Implementations;

public class DefaultPermissionProvider : IServerPermissionProvider
{
    public Task<ServerPermission[]> GetPermissions(ServerDetailResponse server)
    {
        ServerPermission[] permissions = [
            new()
            {
                Identifier = ServerPermissionConstants.Console,
                Description = "Allows access to the console",
                DisplayName = "Console",
                Icon = "icon-square-terminal"
            },
            new()
            {
                Identifier = ServerPermissionConstants.Files,
                Description = "Allows access to the files",
                DisplayName = "Files",
                Icon = "icon-folder"
            },
            new()
            {
                Identifier = ServerPermissionConstants.Power,
                Description = "Allows actions like turning off the server and starting it",
                DisplayName = "Power",
                Icon = "icon-power"
            },
            new()
            {
                Identifier = ServerPermissionConstants.Settings,
                Description = "Gives permissions to perform re-installs or change other general settings",
                DisplayName = "Settings",
                Icon = "icon-settings"
            },
            new()
            {
                Identifier = ServerPermissionConstants.Shares,
                Description = "Dangerous! This allows control to the available shares and their access level for a server",
                DisplayName = "Shares",
                Icon = "icon-users"
            },
            new()
            {
                Identifier = ServerPermissionConstants.Variables,
                Description = "Allows access to the server software specific settings",
                DisplayName = "Variables",
                Icon = "icon-variable"
            }
        ];
        
        return Task.FromResult(permissions);
    }
}