using System.Net;
using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Files;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Files;

namespace MoonlightServers.Frontend.Services;

[Scoped]
public class ServerFileSystemService
{
    private readonly HttpApiClient ApiClient;

    public ServerFileSystemService(HttpApiClient apiClient)
    {
        ApiClient = apiClient;
    }

    public async Task<ServerFilesEntryResponse[]> List(int serverId, string path)
    {
        return await ApiClient.GetJson<ServerFilesEntryResponse[]>(
            $"api/client/servers/{serverId}/files/list?path={path}"
        );
    }

    public async Task Move(int serverId, string oldPath, string newPath)
    {
        await ApiClient.Post(
            $"api/client/servers/{serverId}/files/move?oldPath={oldPath}&newPath={newPath}"
        );
    }

    public async Task Delete(int serverId, string path)
    {
        await ApiClient.Delete(
            $"api/client/servers/{serverId}/files/delete?path={path}"
        );
    }

    public async Task Mkdir(int serverId, string path)
    {
        await ApiClient.Post(
            $"api/client/servers/{serverId}/files/mkdir?path={path}"
        );
    }
    
    public async Task Touch(int serverId, string path)
    {
        await ApiClient.Post(
            $"api/client/servers/{serverId}/files/touch?path={path}"
        );
    }

    public async Task<ServerFilesUploadResponse> Upload(int serverId)
    {
        return await ApiClient.GetJson<ServerFilesUploadResponse>(
            $"api/client/servers/{serverId}/files/upload"
        );
    }

    public async Task<ServerFilesDownloadResponse> Download(int serverId, string path)
    {
        return await ApiClient.GetJson<ServerFilesDownloadResponse>(
            $"api/client/servers/{serverId}/files/download?path={path}"
        );
    }

    public async Task Compress(int serverId, string type, string[] items, string destination)
    {
        await ApiClient.Post(
            $"api/client/servers/{serverId}/files/compress",
            new ServerFilesCompressRequest()
            {
                Type = type,
                Items = items,
                Destination = destination
            }
        );
    }
    
    public async Task Decompress(int serverId, string type, string path, string destination)
    {
        await ApiClient.Post(
            $"api/client/servers/{serverId}/files/decompress",
            new ServerFilesDecompressRequest()
            {
                Type = type,
                Path = path,
                Destination = destination
            }
        );
    }
}