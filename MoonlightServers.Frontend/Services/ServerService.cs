using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;
using MoonlightServers.Shared.Http.Responses.Client.Servers;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Variables;

namespace MoonlightServers.Frontend.Services;

[Scoped]
public class ServerService
{
    private readonly HttpApiClient HttpApiClient;

    public ServerService(HttpApiClient httpApiClient)
    {
        HttpApiClient = httpApiClient;
    }

    public async Task<PagedData<ServerDetailResponse>> GetServers(int page, int perPage)
    {
        return await HttpApiClient.GetJson<PagedData<ServerDetailResponse>>(
            $"api/client/servers?page={page}&pageSize={perPage}"
        );
    }
    
    public async Task<PagedData<ServerDetailResponse>> GetSharedServers(int page, int perPage)
    {
        return await HttpApiClient.GetJson<PagedData<ServerDetailResponse>>(
            $"api/client/servers/shared?page={page}&pageSize={perPage}"
        );
    }

    public async Task<ServerDetailResponse> GetServer(int serverId)
    {
        return await HttpApiClient.GetJson<ServerDetailResponse>(
            $"api/client/servers/{serverId}"
        );
    }

    public async Task<ServerStatusResponse> GetStatus(int serverId)
    {
        return await HttpApiClient.GetJson<ServerStatusResponse>(
            $"api/client/servers/{serverId}/status"
        );
    }

    public async Task<ServerLogsResponse> GetLogs(int serverId)
    {
        return await HttpApiClient.GetJson<ServerLogsResponse>(
            $"api/client/servers/{serverId}/logs"
        );
    }

    public async Task<ServerStatsResponse> GetStats(int serverId)
    {
        return await HttpApiClient.GetJson<ServerStatsResponse>(
            $"api/client/servers/{serverId}/stats"
        );
    }

    public async Task<ServerWebSocketResponse> GetWebSocket(int serverId)
    {
        return await HttpApiClient.GetJson<ServerWebSocketResponse>(
            $"api/client/servers/{serverId}/ws"
        );
    }

    public async Task Install(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/install"
        );
    }

    #region Power actions

    public async Task Start(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/start"
        );
    }

    public async Task Stop(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/stop"
        );
    }

    public async Task Kill(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/kill"
        );
    }

    #endregion

    #region Variables

    public async Task<ServerVariableDetailResponse[]> GetVariables(int serverId)
    {
        return await HttpApiClient.GetJson<ServerVariableDetailResponse[]>(
            $"api/client/servers/{serverId}/variables"
        );
    }

    public async Task UpdateVariables(int serverId, UpdateServerVariableRangeRequest request)
    {
        await HttpApiClient.Patch(
            $"api/client/servers/{serverId}/variables",
            request
        );
    }
    
    public async Task UpdateVariable(int serverId, UpdateServerVariableRequest request)
    {
        await HttpApiClient.Put(
            $"api/client/servers/{serverId}/variables",
            request
        );
    }

    #endregion
}