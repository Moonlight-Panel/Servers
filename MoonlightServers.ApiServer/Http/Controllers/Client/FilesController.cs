using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.DaemonShared.Enums;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Files;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Files;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class FilesController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly ServerFileSystemService ServerFileSystemService;
    private readonly NodeService NodeService;
    private readonly ServerAuthorizeService AuthorizeService;

    public FilesController(
        DatabaseRepository<Server> serverRepository,
        ServerFileSystemService serverFileSystemService,
        NodeService nodeService,
        ServerAuthorizeService authorizeService
    )
    {
        ServerRepository = serverRepository;
        ServerFileSystemService = serverFileSystemService;
        NodeService = nodeService;
        AuthorizeService = authorizeService;
    }

    [HttpGet("{serverId:int}/files/list")]
    public async Task<ServerFilesEntryResponse[]> List([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionType.Read);

        var entries = await ServerFileSystemService.List(server, path);

        return entries.Select(x => new ServerFilesEntryResponse()
        {
            Name = x.Name,
            Size = x.Size,
            IsFile = x.IsFile,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }

    [HttpPost("{serverId:int}/files/move")]
    public async Task Move([FromRoute] int serverId, [FromQuery] string oldPath, [FromQuery] string newPath)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        await ServerFileSystemService.Move(server, oldPath, newPath);
    }

    [HttpDelete("{serverId:int}/files/delete")]
    public async Task Delete([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        await ServerFileSystemService.Delete(server, path);
    }

    [HttpPost("{serverId:int}/files/mkdir")]
    public async Task Mkdir([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        await ServerFileSystemService.Mkdir(server, path);
    }

    [HttpGet("{serverId:int}/files/upload")]
    public async Task<ServerFilesUploadResponse> Upload([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        var accessToken = NodeService.CreateAccessToken(
            server.Node,
            parameters =>
            {
                parameters.Add("type", "upload");
                parameters.Add("serverId", server.Id);
            },
            TimeSpan.FromMinutes(1)
        );

        var url = "";

        url += server.Node.UseSsl ? "https://" : "http://";
        url += $"{server.Node.Fqdn}:{server.Node.HttpPort}/";
        url += $"api/servers/upload?access_token={accessToken}";

        return new ServerFilesUploadResponse()
        {
            UploadUrl = url
        };
    }

    [HttpGet("{serverId:int}/files/download")]
    public async Task<ServerFilesDownloadResponse> Download([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionType.Read);

        var accessToken = NodeService.CreateAccessToken(
            server.Node,
            parameters =>
            {
                parameters.Add("type", "download");
                parameters.Add("path", path);
                parameters.Add("serverId", server.Id);
            },
            TimeSpan.FromMinutes(1)
        );

        var url = "";

        url += server.Node.UseSsl ? "https://" : "http://";
        url += $"{server.Node.Fqdn}:{server.Node.HttpPort}/";
        url += $"api/servers/download?access_token={accessToken}";

        return new ServerFilesDownloadResponse()
        {
            DownloadUrl = url
        };
    }

    [HttpPost("{serverId:int}/files/compress")]
    public async Task Compress([FromRoute] int serverId, [FromBody] ServerFilesCompressRequest request)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        if (!Enum.TryParse(request.Type, true, out CompressType type))
            throw new HttpApiException("Invalid compress type provided", 400);

        await ServerFileSystemService.Compress(server, type, request.Items, request.Destination);
    }

    [HttpPost("{serverId:int}/files/decompress")]
    public async Task Decompress([FromRoute] int serverId, [FromBody] ServerFilesDecompressRequest request)
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        if (!Enum.TryParse(request.Type, true, out CompressType type))
            throw new HttpApiException("Invalid compress type provided", 400);

        await ServerFileSystemService.Decompress(server, type, request.Path, request.Destination);
    }

    private async Task<Server> GetServerById(int serverId, ServerPermissionType type)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var authorizeResult = await AuthorizeService.Authorize(
            User, server,
            permission => permission.Name == "files" && permission.Type >= type
        );

        if (!authorizeResult.Succeeded)
            throw new HttpApiException("No permission for the requested resource", 403);

        return server;
    }
}