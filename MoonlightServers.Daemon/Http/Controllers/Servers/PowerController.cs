using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Controllers.Servers;

[ApiController]
[Route("api/servers/{id:int}")]
public class PowerController : Controller
{
    private readonly ServerService ServerService;

    public PowerController(ServerService serverService)
    {
        ServerService = serverService;
    }

    [HttpPost("start")]
    public async Task<ActionResult> StartAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        if (!server.StateMachine.CanFire(ServerTrigger.Start))
            return Problem("Cannot fire start trigger in this state");

        await server.StateMachine.FireAsync(ServerTrigger.Start);
        return NoContent();
    }
    
    [HttpPost("stop")]
    public async Task<ActionResult> StopAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        if (!server.StateMachine.CanFire(ServerTrigger.Stop))
            return Problem("Cannot fire stop trigger in this state");

        await server.StateMachine.FireAsync(ServerTrigger.Stop);
        return NoContent();
    }
    
    [HttpPost("kill")]
    public async Task<ActionResult> KillAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        if (!server.StateMachine.CanFire(ServerTrigger.Kill))
            return Problem("Cannot fire kill trigger in this state");

        await server.StateMachine.FireAsync(ServerTrigger.Kill);
        return NoContent();
    }
    
    [HttpPost("install")]
    public async Task<ActionResult> InstallAsync([FromRoute] int id)
    {
        var server = ServerService.GetById(id);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        if (!server.StateMachine.CanFire(ServerTrigger.Install))
            return Problem("Cannot fire install trigger in this state");

        await server.StateMachine.FireAsync(ServerTrigger.Install);
        return NoContent();
    }
}