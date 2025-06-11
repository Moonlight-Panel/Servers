using MoonlightServers.Shared.Models;

namespace MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;

public record UpdateShareRequest
{
    public List<ServerSharePermission> Permissions { get; set; } = [];
}