using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Shares;

namespace MoonlightServers.Frontend.Services;

[Scoped]
public class ServerShareService
{
    private readonly HttpApiClient ApiClient;

    public ServerShareService(HttpApiClient apiClient)
    {
        ApiClient = apiClient;
    }

    public async Task<PagedData<ServerShareResponse>> Get(int id, int page, int pageSize)
        => await ApiClient.GetJson<PagedData<ServerShareResponse>>(
            $"api/client/servers/{id}/shares?page={page}&pageSize={pageSize}");

    public async Task<ServerShareResponse> Get(int id, int shareId)
        => await ApiClient.GetJson<ServerShareResponse>($"api/client/servers/{id}/shares/{shareId}");

    public async Task<ServerShareResponse> Create(int id, CreateShareRequest request)
        => await ApiClient.PostJson<ServerShareResponse>($"api/client/servers/{id}/shares", request);

    public async Task<ServerShareResponse> Update(int id, int shareId, UpdateShareRequest request)
        => await ApiClient.PatchJson<ServerShareResponse>($"api/client/servers/{id}/shares/{shareId}", request);

    public async Task Delete(int id, int shareId)
        => await ApiClient.Delete($"api/client/servers/{id}/shares/{shareId}");
}