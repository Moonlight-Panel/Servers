using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.ServerSystem.SubSystems;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Requests;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[Authorize]
[ApiController]
[Route("api/servers")]
public class ServerFileSystemController : Controller
{
    private readonly ServerService ServerService;

    public ServerFileSystemController(ServerService serverService)
    {
        ServerService = serverService;
    }
    
    [HttpGet("{id:int}/files/list")]
    public async Task<ServerFileSystemResponse[]> List([FromRoute] int id, [FromQuery] string path = "")
    {
        var fileSystem = await GetFileSystemById(id);

        return await fileSystem.List(path);
    }

    [HttpPost("{id:int}/files/move")]
    public async Task Move([FromRoute] int id, [FromQuery] string oldPath, [FromQuery] string newPath)
    {
        var fileSystem = await GetFileSystemById(id);

        await fileSystem.Move(oldPath, newPath);
    }
    
    [HttpDelete("{id:int}/files/delete")]
    public async Task Delete([FromRoute] int id, [FromQuery] string path)
    {
        var fileSystem = await GetFileSystemById(id);

        await fileSystem.Delete(path);
    }
    
    [HttpPost("{id:int}/files/mkdir")]
    public async Task Mkdir([FromRoute] int id, [FromQuery] string path)
    {
        var fileSystem = await GetFileSystemById(id);

        await fileSystem.Mkdir(path);
    }

    [HttpPost("{id:int}/files/compress")]
    public async Task Compress([FromRoute] int id, [FromBody] ServerFilesCompressRequest request)
    {
        var fileSystem = await GetFileSystemById(id);

        await fileSystem.Compress(
            request.Items,
            request.Destination,
            request.Type
        );
    }
    
    [HttpPost("{id:int}/files/decompress")]
    public async Task Decompress([FromRoute] int id, [FromBody] ServerFilesDecompressRequest request)
    {
        var fileSystem = await GetFileSystemById(id);

        await fileSystem.Decompress(
            request.Path,
            request.Destination,
            request.Type
        );
    }

    private async Task<ServerFileSystem> GetFileSystemById(int serverId)
    {
        var server = ServerService.Find(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var storageSubSystem = server.GetRequiredSubSystem<StorageSubSystem>();

        return await storageSubSystem.GetFileSystem();
    }
}