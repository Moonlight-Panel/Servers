using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;

public record UpdateShareRequest
{
    public Dictionary<string, ServerPermissionLevel> Permissions { get; set; } = [];
}