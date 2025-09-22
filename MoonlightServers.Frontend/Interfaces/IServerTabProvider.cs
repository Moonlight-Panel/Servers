using MoonlightServers.Frontend.Models;
using MoonlightServers.Shared.Http.Responses.Client.Servers;

namespace MoonlightServers.Frontend.Interfaces;

public interface IServerTabProvider
{
    public Task<ServerTab[]> GetTabsAsync(ServerDetailResponse server);
}