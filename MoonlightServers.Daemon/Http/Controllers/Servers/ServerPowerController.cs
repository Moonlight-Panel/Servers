using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[Authorize]
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
    public async Task Start(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Start();
    }
    
    [HttpPost("{serverId:int}/stop")]
    public async Task Stop(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Stop();
    }
    
    [HttpPost("{serverId:int}/install")]
    public async Task Install(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Install();
    }
    
    [HttpPost("{serverId:int}/kill")]
    public async Task Kill(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.Kill();
    }
}