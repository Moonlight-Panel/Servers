using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Interfaces;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Implementations.ServerAuthFilters;

public class ShareAuthFilter : IServerAuthorizationFilter
{
    private readonly DatabaseRepository<ServerShare> ShareRepository;

    public ShareAuthFilter(DatabaseRepository<ServerShare> shareRepository)
    {
        ShareRepository = shareRepository;
    }

    public int Priority => 0;

    public async Task<ServerAuthorizationResult?> Process(
        ClaimsPrincipal user,
        Server server,
        string permissionId,
        ServerPermissionLevel requiredLevel
    )
    {
        var userIdValue = user.FindFirstValue("userId");

        if (string.IsNullOrEmpty(userIdValue))
            return null;

        var userId = int.Parse(userIdValue);

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.UserId == userId);

        if (share == null)
            return null;

        if (string.IsNullOrEmpty(permissionId) || requiredLevel == ServerPermissionLevel.None)
            return ServerAuthorizationResult.Success(share);

        if (
            share.Content.Permissions.TryGetValue(permissionId, out var shareLevel) &&
            shareLevel >= requiredLevel
        )
        {
            return ServerAuthorizationResult.Success(share);
        }

        return null;
    }
}