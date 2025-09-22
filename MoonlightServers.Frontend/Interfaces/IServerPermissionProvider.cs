using MoonlightServers.Frontend.Models;
using MoonlightServers.Shared.Http.Responses.Client.Servers;

namespace MoonlightServers.Frontend.Interfaces;

public interface IServerPermissionProvider
{
    public Task<ServerPermission[]> GetPermissionsAsync(ServerDetailResponse server);
}