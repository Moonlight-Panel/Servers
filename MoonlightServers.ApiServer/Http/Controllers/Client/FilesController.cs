using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.DaemonShared.Enums;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Files;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Files;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers/{serverId:int}/files")]
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

    [HttpGet("list")]
    public async Task<ActionResult<ServerFilesEntryResponse[]>> List([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.Read);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var entries = await ServerFileSystemService.List(server.Value, path);

        return entries.Select(x => new ServerFilesEntryResponse()
        {
            Name = x.Name,
            Size = x.Size,
            IsFolder = x.IsFolder,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }

    [HttpPost("move")]
    public async Task<ActionResult> Move([FromRoute] int serverId, [FromQuery] string oldPath, [FromQuery] string newPath)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        await ServerFileSystemService.Move(server.Value, oldPath, newPath);
        return NoContent();
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> Delete([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        await ServerFileSystemService.Delete(server.Value, path);
        return NoContent();
    }

    [HttpPost("mkdir")]
    public async Task<ActionResult> Mkdir([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        await ServerFileSystemService.Mkdir(server.Value, path);
        return NoContent();
    }

    [HttpPost("touch")]
    public async Task<ActionResult> Touch([FromRoute] int serverId, [FromQuery] string path)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        await ServerFileSystemService.Mkdir(server.Value, path);
        return NoContent();
    }

    [HttpGet("upload")]
    public async Task<ActionResult<ServerFilesUploadResponse>> Upload([FromRoute] int serverId)
    {
        var serverResult = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (serverResult.Value == null)
            return serverResult.Result ?? Problem("Unable to retrieve server");

        var server = serverResult.Value;

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

    [HttpGet("download")]
    public async Task<ActionResult<ServerFilesDownloadResponse>> Download([FromRoute] int serverId, [FromQuery] string path)
    {
        var serverResult = await GetServerById(serverId, ServerPermissionLevel.Read);
        
        if (serverResult.Value == null)
            return serverResult.Result ?? Problem("Unable to retrieve server");

        var server = serverResult.Value;

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

    [HttpPost("compress")]
    public async Task<ActionResult> Compress([FromRoute] int serverId, [FromBody] ServerFilesCompressRequest request)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        if (!Enum.TryParse(request.Type, true, out CompressType type))
            return Problem("Invalid compress type provided", statusCode: 400);

        await ServerFileSystemService.Compress(server.Value, type, request.Items, request.Destination);
        return Ok();
    }

    [HttpPost("decompress")]
    public async Task<ActionResult> Decompress([FromRoute] int serverId, [FromBody] ServerFilesDecompressRequest request)
    {
        var server = await GetServerById(serverId, ServerPermissionLevel.ReadWrite);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        if (!Enum.TryParse(request.Type, true, out CompressType type))
            return Problem("Invalid decompress type provided", statusCode: 400);

        await ServerFileSystemService.Decompress(server.Value, type, request.Path, request.Destination);
        return NoContent();
    }

    private async Task<ActionResult<Server>> GetServerById(int serverId, ServerPermissionLevel level)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.Authorize(
            User, server,
            ServerPermissionConstants.Files,
            level
        );

        if (!authorizeResult.Succeeded)
        {
            return Problem(
                authorizeResult.Message ?? "No permission for the requested resource",
                statusCode: 403
            );
        }

        return server;
    }
}