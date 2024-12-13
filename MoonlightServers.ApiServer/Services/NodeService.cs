using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
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

    public async Task<SystemInfoResponse> GetSystemInfo(Node node)
    {
        using var apiClient = await CreateApiClient(node);

        return await apiClient.GetJson<SystemInfoResponse>("api/system/info");
    }
    
    public async Task<SystemStatusResponse> GetSystemStatus(Node node)
    {
        using var apiClient = await CreateApiClient(node);

        return await apiClient.GetJson<SystemStatusResponse>("api/system/status");
    }

    public async Task<SystemDataUsageResponse> GetSystemDataUsage(Node node)
    {
        using var apiClient = await CreateApiClient(node);

        return await apiClient.GetJson<SystemDataUsageResponse>("api/system/dataUsage");
    }
}