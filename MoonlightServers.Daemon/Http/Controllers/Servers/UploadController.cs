using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[AllowAnonymous]
[Route("api/servers/upload")]
public class UploadController : Controller
{
    private readonly AccessTokenHelper AccessTokenHelper;
    private readonly AppConfiguration Configuration;
    private readonly ServerService ServerService;

    public UploadController(
        AccessTokenHelper accessTokenHelper,
        ServerService serverService,
        AppConfiguration configuration
    )
    {
        AccessTokenHelper = accessTokenHelper;
        ServerService = serverService;
        Configuration = configuration;
    }

    [HttpPost]
    public async Task Upload([FromQuery] string token)
    {
        var file = Request.Form.Files.FirstOrDefault();

        if (file == null)
            throw new HttpApiException("No file provided", 400);
        
        if(file.Length > ByteConverter.FromMegaBytes(Configuration.Files.UploadLimit).Bytes)
            throw new HttpApiException("The provided file is bigger than the upload limit", 400);

        #region Token validation

        if (!AccessTokenHelper.Process(token, out var claims))
            throw new HttpApiException("Invalid access token provided", 401);

        var typeClaim = claims.FirstOrDefault(x => x.Type == "type");

        if (typeClaim == null || typeClaim.Value != "upload")
            throw new HttpApiException("Invalid access token provided: Missing or invalid type", 401);

        var serverIdClaim = claims.FirstOrDefault(x => x.Type == "serverId");

        if (serverIdClaim == null || !int.TryParse(serverIdClaim.Value, out var serverId))
            throw new HttpApiException("Invalid access token provided: Missing or invalid server id", 401);

        #endregion

        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var dataStream = file.OpenReadStream();
        var path = file.FileName;

        await server.FileSystem.Create(path, dataStream);
    }
}