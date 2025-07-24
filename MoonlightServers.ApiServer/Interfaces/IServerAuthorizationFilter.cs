using System.Security.Claims;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Interfaces;

public interface IServerAuthorizationFilter
{
    // Return null => skip to next filter / handler
    // Return any value, instant complete

    public int Priority { get; }
    
    public Task<ServerAuthorizationResult?> Process(
        ClaimsPrincipal user,
        Server server,
        string permissionId,
        ServerPermissionLevel requiredLevel
    );
}