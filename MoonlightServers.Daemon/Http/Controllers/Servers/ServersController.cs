using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
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
            State = (ServerState)server.State
        };
    }

    [HttpGet("{serverId:int}/logs")]
    public async Task<ServerLogsResponse> GetLogs([FromRoute] int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        return new ServerLogsResponse()
        {
            Messages = await server.GetConsoleMessages()
        };
    }
}