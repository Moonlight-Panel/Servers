using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class ServerAuthorizeService
{
    private readonly IAuthorizationService AuthorizationService;
    private readonly DatabaseRepository<ServerShare> ShareRepository;

    public ServerAuthorizeService(
        IAuthorizationService authorizationService,
        DatabaseRepository<ServerShare> shareRepository
    )
    {
        AuthorizationService = authorizationService;
        ShareRepository = shareRepository;
    }

    public async Task<AuthorizationResult> Authorize(ClaimsPrincipal user, Server server, Func<ServerSharePermission, bool>? filter = null)
    {
        var userIdClaim = user.FindFirst("userId");

        // User specific authorization
        if (userIdClaim != null)
        {
            var result = await AuthorizeViaUser(userIdClaim, server, filter);

            if (result.Succeeded)
                return result;
        }
        
        // Permission specific authorization
        return await AuthorizeViaPermission(user);
    }

    private async Task<AuthorizationResult> AuthorizeViaUser(Claim userIdClaim, Server server, Func<ServerSharePermission, bool>? filter = null)
    {
        var userId = int.Parse(userIdClaim.Value);

        if (server.OwnerId == userId)
            return AuthorizationResult.Success();

        var possibleShare = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.UserId == userId);

        if (possibleShare == null)
            return AuthorizationResult.Failed();

        // If no filter has been specified every server share is valid
        // no matter which permission the share actually has
        if (filter == null)
            return AuthorizationResult.Success();

        if(possibleShare.Content.Permissions.Any(filter))
            return AuthorizationResult.Success();

        return AuthorizationResult.Failed();
    }

    private async Task<AuthorizationResult> AuthorizeViaPermission(ClaimsPrincipal user)
    {
        return await AuthorizationService.AuthorizeAsync(
            user,
            "permissions:admin.servers.get"
        );
    }

    private ServerSharePermission[] ParsePermissions(string permissionsString)
    {
        var result = new List<ServerSharePermission>();
        
        var permissions = permissionsString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var permission in permissions)
        {
            var permissionParts = permission.Split(':', StringSplitOptions.RemoveEmptyEntries);
            
            // Skipped malformed permission parts
            if(permissionParts.Length != 2)
                continue;
            
            if(!Enum.TryParse(permissionParts[1], true, out ServerPermissionType permissionType))
                continue;
            
            result.Add(new()
            {
                Name = permissionParts[0],
                Type = permissionType
            });
        }

        return result.ToArray();
    }
}