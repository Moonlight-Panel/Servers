using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes.Statistics;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes.Sys;

namespace MoonlightServers.Frontend.Services;

[Scoped]
public class NodeService
{
    private readonly HttpApiClient HttpApiClient;

    public NodeService(HttpApiClient httpApiClient)
    {
        HttpApiClient = httpApiClient;
    }

    public async Task<NodeSystemStatusResponse> GetSystemStatusAsync(int nodeId)
    {
        return await HttpApiClient.GetJson<NodeSystemStatusResponse>($"api/admin/servers/nodes/{nodeId}/system/status");
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(int nodeId)
    {
        return await HttpApiClient.GetJson<StatisticsResponse>(
            $"api/admin/servers/nodes/{nodeId}/statistics"
        );
    }
    
    public async Task<DockerStatisticsResponse> GetDockerStatisticsAsync(int nodeId)
    {
        return await HttpApiClient.GetJson<DockerStatisticsResponse>(
            $"api/admin/servers/nodes/{nodeId}/statistics/docker"
        );
    }
}