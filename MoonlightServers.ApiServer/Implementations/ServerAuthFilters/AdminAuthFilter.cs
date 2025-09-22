using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Implementations.ServerAuthFilters;

public class AdminAuthFilter : IServerAuthorizationFilter
{
    private readonly IAuthorizationService AuthorizationService;

    public int Priority => 0;

    public AdminAuthFilter(IAuthorizationService authorizationService)
    {
        AuthorizationService = authorizationService;
    }

    public async Task<ServerAuthorizationResult?> ProcessAsync(
        ClaimsPrincipal user,
        Server server,
        string permissionId,
        ServerPermissionLevel requiredLevel
    )
    {
        var authResult = await AuthorizationService.AuthorizeAsync(
            user,
            "permissions:admin.servers.manage"
        );

        return authResult.Succeeded ? ServerAuthorizationResult.Success() : null;
    }
}