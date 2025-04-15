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
        [FromQuery] long totalSize,
        [FromQuery] int chunkId,
        [FromQuery] string path
    )
    {
        var chunkSize = ByteConverter.FromMegaBytes(Configuration.Files.UploadChunkSize).Bytes;
        var uploadLimit = ByteConverter.FromMegaBytes(Configuration.Files.UploadSizeLimit).Bytes;
        
        #region File validation

        if (Request.Form.Files.Count != 1)
            throw new HttpApiException("You need to provide exactly one file", 400);

        var file = Request.Form.Files[0];
        
        if (file.Length > chunkSize)
            throw new HttpApiException("The provided data exceeds the chunk size limit", 400);

        #endregion

        var serverId = int.Parse(User.Claims.First(x => x.Type == "serverId").Value);

        #region Chunk calculation and validation
        
        if(totalSize > uploadLimit)
            throw new HttpApiException("Invalid upload request: Exceeding upload limit", 400);

        var chunks = totalSize / chunkSize;
        chunks += totalSize % chunkSize > 0 ? 1 : 0;

        if (chunkId > chunks)
            throw new HttpApiException("Invalid chunk id: Out of bounds", 400);
        
        var positionToSkipTo = chunkSize * chunkId;

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