using System.Security.Claims;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Interfaces;

public interface IServerAuthorizationFilter
{
    // Return null => skip to next filter / handler
    // Return any value, instant return
    public Task<ServerAuthorizationResult?> Process(
        ClaimsPrincipal user,
        Server server,
        Func<ServerSharePermission, bool>? filter = null
    );
}