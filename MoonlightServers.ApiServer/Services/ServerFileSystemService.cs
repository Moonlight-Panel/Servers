using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.DaemonSide.Http.Requests;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class ServerFileSystemService
{
    private readonly NodeService NodeService;
    private readonly DatabaseRepository<Server> ServerRepository;

    public ServerFileSystemService(
        NodeService nodeService,
        DatabaseRepository<Server> serverRepository
    )
    {
        NodeService = nodeService;
        ServerRepository = serverRepository;
    }

    public async Task<ServerFileSystemResponse[]> List(Server server, string path)
    {
        using var apiClient = await GetApiClient(server);
        
        return await apiClient.GetJson<ServerFileSystemResponse[]>(
            $"api/servers/{server.Id}/files/list?path={path}"
        );
    }

    public async Task Move(Server server, string oldPath, string newPath)
    {
        using var apiClient = await GetApiClient(server);

        await apiClient.Post(
            $"api/servers/{server.Id}/files/move?oldPath={oldPath}&newPath={newPath}"
        );
    }
    
    public async Task Delete(Server server, string path)
    {
        using var apiClient = await GetApiClient(server);

        await apiClient.Delete(
            $"api/servers/{server.Id}/files/delete?path={path}"
        );
    }
    
    public async Task Mkdir(Server server, string path)
    {
        using var apiClient = await GetApiClient(server);

        await apiClient.Post(
            $"api/servers/{server.Id}/files/mkdir?path={path}"
        );
    }

    public async Task Compress(Server server, CompressType type, string[] items, string destination)
    {
        using var apiClient = await GetApiClient(server);

        await apiClient.Post(
            $"api/servers/{server.Id}/files/compress",
            new ServerFilesCompressRequest()
            {
                Type = type,
                Items = items,
                Destination = destination
            }
        );
    }
    
    public async Task Decompress(Server server, CompressType type, string path, string destination)
    {
        using var apiClient = await GetApiClient(server);

        await apiClient.Post(
            $"api/servers/{server.Id}/files/decompress",
            new ServerFilesDecompressRequest()
            {
                Type = type,
                Path = path,
                Destination = destination
            }
        );
    }

    #region Helpers

    private async Task<HttpApiClient> GetApiClient(Server server)
    {
        var serverWithNode = server;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // It can be null when its not included when loading via ef !!!
        if (server.Node == null)
        {
            serverWithNode = await ServerRepository
                .Get()
                .Include(x => x.Node)
                .FirstAsync(x => x.Id == server.Id);
        }

        return NodeService.CreateApiClient(serverWithNode.Node);
    }

    #endregion
}