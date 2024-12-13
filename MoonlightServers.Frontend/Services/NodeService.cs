using MoonCore.Attributes;
using MoonCore.Helpers;
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

    public async Task<NodeSystemStatusResponse> GetSystemStatus(int nodeId)
    {
        return await HttpApiClient.GetJson<NodeSystemStatusResponse>($"api/admin/servers/nodes/{nodeId}/system/status");
    }
}