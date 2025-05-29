using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.ServerSystem.SubSystems;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers/download")]
[Authorize(AuthenticationSchemes = "accessToken", Policy = "serverDownload")]
public class DownloadController : Controller
{
    private readonly ServerService ServerService;

    public DownloadController(ServerService serverService)
    {
        ServerService = serverService;
    }

    [HttpGet]
    public async Task Download()
    {
        var serverId = int.Parse(User.Claims.First(x => x.Type == "serverId").Value);
        var path = User.Claims.First(x => x.Type == "path").Value;

        var server = ServerService.Find(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var storageSubSystem = server.GetRequiredSubSystem<StorageSubSystem>();

        var fileSystem = await storageSubSystem.GetFileSystem();

        await fileSystem.Read(
            path,
            async dataStream =>
            {
                await Results.File(dataStream).ExecuteAsync(HttpContext);
            }
        );
    }
}