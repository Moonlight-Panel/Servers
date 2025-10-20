using MoonCore.Attributes;
using MoonCore.Common;
using MoonCore.Helpers;
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

    public async Task<CountedData<ServerShareResponse>> GetAsync(int id, int startIndex, int count)
        => await ApiClient.GetJson<CountedData<ServerShareResponse>>(
            $"api/client/servers/{id}/shares?startIndex={startIndex}&count={count}");

    public async Task<ServerShareResponse> GetAsync(int id, int shareId)
        => await ApiClient.GetJson<ServerShareResponse>($"api/client/servers/{id}/shares/{shareId}");

    public async Task<ServerShareResponse> CreateAsync(int id, CreateShareRequest request)
        => await ApiClient.PostJson<ServerShareResponse>($"api/client/servers/{id}/shares", request);

    public async Task<ServerShareResponse> UpdateAsync(int id, int shareId, UpdateShareRequest request)
        => await ApiClient.PatchJson<ServerShareResponse>($"api/client/servers/{id}/shares/{shareId}", request);

    public async Task DeleteAsync(int id, int shareId)
        => await ApiClient.Delete($"api/client/servers/{id}/shares/{shareId}");
}