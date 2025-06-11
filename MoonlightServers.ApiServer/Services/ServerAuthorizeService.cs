using System.Security.Claims;
using MoonCore.Attributes;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class ServerAuthorizeService
{
    private readonly IEnumerable<IServerAuthorizationFilter> AuthorizationFilters;

    public ServerAuthorizeService(
        IEnumerable<IServerAuthorizationFilter> authorizationFilters
    )
    {
        AuthorizationFilters = authorizationFilters;
    }

    public async Task<ServerAuthorizationResult> Authorize(
        ClaimsPrincipal user,
        Server server,
        Func<ServerSharePermission, bool>? filter = null
    )
    {
        foreach (var authorizationFilter in AuthorizationFilters)
        {
            var result = await authorizationFilter.Process(user, server, filter);

            if (result != null)
                return result;
        }
        
        return ServerAuthorizationResult.Failed();
    }
}