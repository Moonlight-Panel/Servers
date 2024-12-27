using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;

    public ServersController(ServerService serverService)
    {
        ServerService = serverService;
    }

    [HttpGet("{serverId:int}/status")]
    public async Task<ServerStatusResponse> GetStatus(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        return new ServerStatusResponse()
        {
            State = server.State
        };
    }

    [HttpPost("{serverId:int}/start")]
    public async Task Start(int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        await server.StateMachine.TransitionTo(ServerState.Starting);
    }
}