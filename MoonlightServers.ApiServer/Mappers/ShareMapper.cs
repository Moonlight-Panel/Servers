using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;
using Riok.Mapperly.Abstractions;

namespace MoonlightServers.ApiServer.Mappers;

[Mapper]
public static partial class ShareMapper
{
    public static ServerShareContent MapToServerShareContent(Dictionary<string, ServerPermissionLevel> permissionLevels)
    {
        return new ServerShareContent()
        {
            Permissions = permissionLevels.Select(x => new ServerShareContent.SharePermission()
            {
                Identifier = x.Key,
                Level = x.Value
            }).ToList()
        };
    }

    public static Dictionary<string, ServerPermissionLevel> MapToPermissionLevels(
        ServerShareContent content)
    {
        return content.Permissions.ToDictionary(x => x.Identifier, x => x.Level);
    }
}