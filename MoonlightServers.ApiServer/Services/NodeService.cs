using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.Http.Responses.Statistics;
using MoonlightServers.DaemonShared.Http.Responses.Sys;

namespace MoonlightServers.ApiServer.Services;

[Singleton]
public class NodeService
{
    public async Task<HttpApiClient> CreateApiClient(Node node)
    {
        string url = "";

        if (node.UseSsl)
            url += "https://";
        else
            url += "http://";

        url += $"{node.Fqdn}:{node.HttpPort}/";
        
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(url)
        };
        
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {node.Token}");

        return new HttpApiClient(httpClient);
    }
    
    public async Task<SystemStatusResponse> GetSystemStatus(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<SystemStatusResponse>("api/system/status");
    }

    #region Statistics

    public async Task<StatisticsApplicationResponse> GetApplicationStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsApplicationResponse>("api/statistics/application");
    }
    
    public async Task<StatisticsHostResponse> GetHostStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsHostResponse>("api/statistics/host");
    }
    
    public async Task<StatisticsDockerResponse> GetDockerStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsDockerResponse>("api/statistics/docker");
    }

    #endregion
}