using MoonlightServers.Frontend.Models;
using MoonlightServers.Shared.Http.Responses.Users.Servers;

namespace MoonlightServers.Frontend.Interfaces;

public interface IServerTabProvider
{
    public Task<ServerTab[]> GetTabs(ServerDetailResponse server);
}