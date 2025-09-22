using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class RemoteService
{
    private readonly HttpApiClient ApiClient;

    public RemoteService(AppConfiguration configuration)
    {
        ApiClient = CreateHttpClient(configuration);
    }

    public async Task GetStatusAsync()
    {
        await ApiClient.Get("api/remote/servers/node/trip");
    }

    public async Task<CountedData<ServerDataResponse>> GetServersAsync(int startIndex, int count)
    {
        return await ApiClient.GetJson<CountedData<ServerDataResponse>>(
            $"api/remote/servers?startIndex={startIndex}&count={count}"
        );
    }

    public async Task<ServerDataResponse> GetServerAsync(int serverId)
    {
        return await ApiClient.GetJson<ServerDataResponse>(
            $"api/remote/servers/{serverId}"
        );
    }

    public async Task<ServerInstallDataResponse> GetServerInstallationAsync(int serverId)
    {
        return await ApiClient.GetJson<ServerInstallDataResponse>(
            $"api/remote/servers/{serverId}/install"
        );
    }

    #region Helpers

    private HttpApiClient CreateHttpClient(AppConfiguration configuration)
    {
        var formattedUrl = configuration.Remote.Url.EndsWith('/')
            ? configuration.Remote.Url
            : configuration.Remote.Url + "/";

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(formattedUrl)
        };

        httpClient.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {configuration.Security.TokenId}.{configuration.Security.Token}"
        );

        return new HttpApiClient(httpClient);
    }

    #endregion
}