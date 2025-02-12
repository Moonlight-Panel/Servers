using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers")]
public class ServerPowerController : Controller
{
    private readonly ServerService ServerService;

    public ServerPowerController(ServerService serverService)
    {
        ServerService = serverService;
    }
    
    [HttpPost("{serverId:int}/start")]
    public async Task Start(int serverId, [FromQuery] bool runAsync = true)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Start();
    }
    
    [HttpPost("{serverId:int}/stop")]
    public async Task Stop(int serverId, [FromQuery] bool runAsync = true)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Stop();
    }
}