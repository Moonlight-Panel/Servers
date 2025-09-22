using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers/{id:int}")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;
    private readonly ServerConfigurationMapper ConfigurationMapper;

    public ServersController(ServerService serverService, ServerConfigurationMapper configurationMapper)
    {
        ServerService = serverService;
        ConfigurationMapper = configurationMapper;
    }

    [HttpPost("sync")]
    public async Task<ActionResult> SyncAsync([FromRoute] int id)
    {
        await ServerService.InitializeByIdAsync(id);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<ServerStatusResponse>> StatusAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        return new ServerStatusResponse()
        {
            State = (ServerState)server.StateMachine.State
        };
    }
    
    [HttpGet("logs")]
    public async Task<ActionResult<ServerLogsResponse>> LogsAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var messages = await server.Console.GetCacheAsync();
        
        return new ServerLogsResponse()
        {
            Messages = messages.ToArray()
        };
    }
    
    [HttpGet("stats")]
    public async Task<ServerStatsResponse> GetStatsAsync([FromRoute] int id)
    {
        return new ServerStatsResponse()
        {

        };
    }
}