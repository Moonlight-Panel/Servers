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

    public async Task<bool> Authorize(ClaimsPrincipal user, Server server, Func<ServerSharePermission, bool>? filter = null)
    {
        var userIdClaim = user.FindFirst("userId");

        // User specific authorization
        if (userIdClaim != null && await AuthorizeViaUser(userIdClaim, server, filter))
            return true;
        
        // Permission specific authorization
        return await AuthorizeViaPermission(user);
    }

    private async Task<bool> AuthorizeViaUser(Claim userIdClaim, Server server, Func<ServerSharePermission, bool>? filter = null)
    {
        var userId = int.Parse(userIdClaim.Value);

        if (server.OwnerId == userId)
            return true;

        var possibleShare = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.UserId == userId);

        if (possibleShare == null)
            return false;

        // If no filter has been specified every server share is valid
        // no matter which permission the share actually has
        if (filter == null)
            return true;

        var permissionsOfShare = ParsePermissions(possibleShare.Permissions);

        return permissionsOfShare.Any(filter);
    }

    private async Task<bool> AuthorizeViaPermission(ClaimsPrincipal user)
    {
        var authorizeResult = await AuthorizationService.AuthorizeAsync(
            user,
            "permissions:admin.servers.get"
        );

        return authorizeResult.Succeeded;
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

    private bool CheckSharePermission(ServerShare share, string permission, ServerPermissionType type)
    {
        if (string.IsNullOrEmpty(share.Permissions))
            return false;

        var permissions = share.Permissions.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var sharePermission in permissions)
        {
            if (!sharePermission.StartsWith(permission))
                continue;

            var typeParts = sharePermission.Split(':', StringSplitOptions.RemoveEmptyEntries);

            // Missing permission type
            if (typeParts.Length != 2)
                return false;

            // Parse type id
            if (!int.TryParse(typeParts[1], out var typeId))
                return false; // Malformed

            var requiredId = (int)type;

            return typeId >= requiredId;
        }

        return false;
    }
}