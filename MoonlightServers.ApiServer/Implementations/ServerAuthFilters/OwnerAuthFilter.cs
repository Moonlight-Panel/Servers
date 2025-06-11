using System.Security.Claims;
using MoonCore.Attributes;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Implementations.ServerAuthFilters;

public class OwnerAuthFilter : IServerAuthorizationFilter
{
    public Task<ServerAuthorizationResult?> Process(ClaimsPrincipal user, Server server, Func<ServerSharePermission, bool>? filter = null)
    {
        var userIdValue = user.FindFirstValue("userId");

        if (string.IsNullOrEmpty(userIdValue)) // This is the case for api keys
            return Task.FromResult<ServerAuthorizationResult?>(null);

        var userId = int.Parse(userIdValue);
        
        if(server.OwnerId != userId)
            return Task.FromResult<ServerAuthorizationResult?>(null);
        
        return Task.FromResult<ServerAuthorizationResult?>(
            ServerAuthorizationResult.Success()
        );
    }
}