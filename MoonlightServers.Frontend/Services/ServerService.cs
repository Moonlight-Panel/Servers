using MoonCore.Attributes;
using MoonCore.Common;
using MoonCore.Helpers;
using MoonlightServers.Shared.Http.Requests.Client.Servers;
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

    public async Task<CountedData<ServerDetailResponse>> GetServersAsync(int startIndex, int count)
    {
        return await HttpApiClient.GetJson<CountedData<ServerDetailResponse>>(
            $"api/client/servers?startIndex={startIndex}&count={count}"
        );
    }
    
    public async Task<CountedData<ServerDetailResponse>> GetSharedServersAsync(int startIndex, int count)
    {
        return await HttpApiClient.GetJson<CountedData<ServerDetailResponse>>(
            $"api/client/servers/shared?startIndex={startIndex}&count={count}"
        );
    }

    public async Task<ServerDetailResponse> GetServerAsync(int serverId)
    {
        return await HttpApiClient.GetJson<ServerDetailResponse>(
            $"api/client/servers/{serverId}"
        );
    }

    public async Task<ServerStatusResponse> GetStatusAsync(int serverId)
    {
        return await HttpApiClient.GetJson<ServerStatusResponse>(
            $"api/client/servers/{serverId}/status"
        );
    }

    public async Task<ServerLogsResponse> GetLogsAsync(int serverId)
    {
        return await HttpApiClient.GetJson<ServerLogsResponse>(
            $"api/client/servers/{serverId}/logs"
        );
    }

    public async Task<ServerStatsResponse> GetStatsAsync(int serverId)
    {
        return await HttpApiClient.GetJson<ServerStatsResponse>(
            $"api/client/servers/{serverId}/stats"
        );
    }

    public async Task RunCommandAsync(int serverId, string command)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/command",
            new ServerCommandRequest()
            {
                Command = command
            }
        );
    }

    public async Task<ServerWebSocketResponse> GetWebSocketAsync(int serverId)
    {
        return await HttpApiClient.GetJson<ServerWebSocketResponse>(
            $"api/client/servers/{serverId}/ws"
        );
    }

    public async Task InstallAsync(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/install"
        );
    }

    #region Power actions

    public async Task StartAsync(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/start"
        );
    }

    public async Task StopAsync(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/stop"
        );
    }

    public async Task KillAsync(int serverId)
    {
        await HttpApiClient.Post(
            $"api/client/servers/{serverId}/kill"
        );
    }

    #endregion

    #region Variables

    public async Task<CountedData<ServerVariableDetailResponse>> GetVariablesAsync(int serverId, int startIndex, int count)
    {
        return await HttpApiClient.GetJson<CountedData<ServerVariableDetailResponse>>(
            $"api/client/servers/{serverId}/variables?startIndex={startIndex}&count={count}"
        );
    }

    public async Task UpdateVariablesAsync(int serverId, UpdateServerVariableRangeRequest request)
    {
        await HttpApiClient.Patch(
            $"api/client/servers/{serverId}/variables",
            request
        );
    }
    
    public async Task UpdateVariableAsync(int serverId, UpdateServerVariableRequest request)
    {
        await HttpApiClient.Put(
            $"api/client/servers/{serverId}/variables",
            request
        );
    }

    #endregion
}