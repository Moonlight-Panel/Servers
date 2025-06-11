using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Attributes;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Implementations.ServerAuthFilters;

public class AdminAuthFilter : IServerAuthorizationFilter
{
    private readonly IAuthorizationService AuthorizationService;

    public AdminAuthFilter(IAuthorizationService authorizationService)
    {
        AuthorizationService = authorizationService;
    }

    public async Task<ServerAuthorizationResult?> Process(
        ClaimsPrincipal user,
        Server server,
        Func<ServerSharePermission, bool>? filter = null
    )
    {
        var authResult = await AuthorizationService.AuthorizeAsync(
            user,
            "permissions:admin.servers.manage"
        );

        return authResult.Succeeded ? ServerAuthorizationResult.Success() : null;
    }
}