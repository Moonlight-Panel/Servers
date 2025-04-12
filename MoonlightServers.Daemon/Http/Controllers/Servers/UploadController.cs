using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers/upload")]
[Authorize(AuthenticationSchemes = "accessToken", Policy = "serverUpload")]
public class UploadController : Controller
{
    private readonly AppConfiguration Configuration;
    private readonly ServerService ServerService;

    private readonly long ChunkSize = ByteConverter.FromMegaBytes(20).Bytes; // TODO config

    public UploadController(
        ServerService serverService,
        AppConfiguration configuration
    )
    {
        ServerService = serverService;
        Configuration = configuration;
    }

    [HttpPost]
    public async Task Upload(
        [FromQuery] long totalSize, // TODO: Add limit in config
        [FromQuery] int chunkId,
        [FromQuery] string path
    )
    {
        #region File validation

        if (Request.Form.Files.Count != 1)
            throw new HttpApiException("You need to provide exactly one file", 400);

        var file = Request.Form.Files[0];
        
        if (file.Length > ChunkSize)
            throw new HttpApiException("The provided data exceeds the chunk size limit", 400);

        #endregion

        var serverId = int.Parse(User.Claims.First(x => x.Type == "serverId").Value);

        #region Chunk calculation and validation

        var chunks = totalSize / ChunkSize;
        chunks += totalSize % ChunkSize > 0 ? 1 : 0;

        if (chunkId > chunks)
            throw new HttpApiException("Invalid chunk id: Out of bounds", 400);
        
        var positionToSkipTo = ChunkSize * chunkId;

        #endregion

        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var dataStream = file.OpenReadStream();

        await server.FileSystem.CreateChunk(
            path,
            totalSize,
            positionToSkipTo,
            dataStream
        );
    }
}