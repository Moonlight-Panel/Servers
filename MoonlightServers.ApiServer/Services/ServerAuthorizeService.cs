using System.Security.Claims;
using MoonCore.Attributes;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class ServerAuthorizeService
{
    private readonly IServerAuthorizationFilter[] AuthorizationFilters;

    public ServerAuthorizeService(
        IEnumerable<IServerAuthorizationFilter> authorizationFilters
    )
    {
        AuthorizationFilters = authorizationFilters.ToArray();
    }

    public async Task<ServerAuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        Server server,
        string permissionIdentifier,
        ServerPermissionLevel permissionLevel
    )
    {
        foreach (var authorizationFilter in AuthorizationFilters)
        {
            var result = await authorizationFilter.ProcessAsync(
                user,
                server,
                permissionIdentifier,
                permissionLevel
            );

            if (result != null)
                return result;
        }

        return ServerAuthorizationResult.Failed();
    }
}