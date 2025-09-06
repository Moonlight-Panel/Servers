using System.Security.Claims;
using MoonCore.Attributes;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Implementations.ServerAuthFilters;

public class OwnerAuthFilter : IServerAuthorizationFilter
{
    public int Priority => 0;

    public Task<ServerAuthorizationResult?> Process(
        ClaimsPrincipal user,
        Server server,
        string permissionId,
        ServerPermissionLevel requiredLevel
    )
    {
        var userIdValue = user.FindFirstValue("UserId");

        if (string.IsNullOrEmpty(userIdValue)) // This is the case for api keys
            return Task.FromResult<ServerAuthorizationResult?>(null);

        var userId = int.Parse(userIdValue);

        if (server.OwnerId != userId)
            return Task.FromResult<ServerAuthorizationResult?>(null);

        return Task.FromResult<ServerAuthorizationResult?>(
            ServerAuthorizationResult.Success()
        );
    }
}