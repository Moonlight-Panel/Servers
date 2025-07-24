using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Models;

public record ServerShareContent
{
    public List<SharePermission> Permissions { get; set; } = new();

    public record SharePermission
    {
        public string Identifier { get; set; }
        public ServerPermissionLevel Level { get; set; }
    }
}