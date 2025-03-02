using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[Authorize]
[ApiController]
[Route("api/servers")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;

    public ServersController(ServerService serverService)
    {
        ServerService = serverService;
    }

    [HttpPost("{serverId:int}/sync")]
    public async Task Sync([FromRoute] int serverId)
    {
        await ServerService.Sync(serverId);
    }

    [HttpDelete("{serverId:int}")]
    public async Task Delete([FromRoute] int serverId)
    {
        await ServerService.Delete(serverId);
    }

    [HttpGet("{serverId:int}/status")]
    public Task<ServerStatusResponse> GetStatus([FromRoute] int serverId)
    {
        var server = ServerService.GetServer(serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        var result = new ServerStatusResponse()
        {
            State = (ServerState)server.State
        };

        return Task.FromResult(result);
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