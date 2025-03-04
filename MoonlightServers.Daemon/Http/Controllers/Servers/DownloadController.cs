using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[AllowAnonymous]
[ApiController]
[Route("api/servers/download")]
public class DownloadController : Controller
{
    private readonly AccessTokenHelper AccessTokenHelper;
    private readonly AppConfiguration Configuration;
    private readonly ServerService ServerService;

    public DownloadController(
        AccessTokenHelper accessTokenHelper,
        ServerService serverService,
        AppConfiguration configuration
    )
    {
        AccessTokenHelper = accessTokenHelper;
        ServerService = serverService;
        Configuration = configuration;
    }

    [HttpGet]
    public async Task Download([FromQuery] string token)
    {
        #region Token validation

        if (!AccessTokenHelper.Process(token, out var claims))
            throw new HttpApiException("Invalid access token provided", 401);

        var typeClaim = claims.FirstOrDefault(x => x.Type == "type");

        if (typeClaim == null || typeClaim.Value != "download")
            throw new HttpApiException("Invalid access token provided: Missing or invalid type", 401);

        var serverIdClaim = claims.FirstOrDefault(x => x.Type == "serverId");

        if (serverIdClaim == null || !int.TryParse(serverIdClaim.Value, out var serverId))
            throw new HttpApiException("Invalid access token provided: Missing or invalid server id", 401);

        var pathClaim = claims.FirstOrDefault(x => x.Type == "path");
        
        if(pathClaim == null || string.IsNullOrEmpty(pathClaim.Value))
            throw new HttpApiException("Invalid access token provided: Missing or invalid path", 401);

        #endregion

        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var path = pathClaim.Value;

        await server.FileSystem.Read(path, async dataStream =>
        {
            await Results.File(dataStream).ExecuteAsync(HttpContext);
        });
    }
}