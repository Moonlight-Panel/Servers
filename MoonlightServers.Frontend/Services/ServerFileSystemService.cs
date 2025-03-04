using MoonCore.Attributes;
using MoonCore.Helpers;
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

    public async Task Upload(int serverId, string path, Stream dataStream)
    {
        var uploadSession = await ApiClient.GetJson<ServerFilesUploadResponse>(
            $"api/client/servers/{serverId}/files/upload"
        );

        using var httpClient = new HttpClient();

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(dataStream), "file", path);
        
        await httpClient.PostAsync(uploadSession.UploadUrl, content);
    }

    public async Task<Stream> Download(int serverId, string path)
    {
        var downloadSession = await ApiClient.GetJson<ServerFilesDownloadResponse>(
            $"api/client/servers/{serverId}/files/download?path={path}"
        );

        using var httpClient = new HttpClient();

        return await httpClient.GetStreamAsync(downloadSession.DownloadUrl);
    }
}