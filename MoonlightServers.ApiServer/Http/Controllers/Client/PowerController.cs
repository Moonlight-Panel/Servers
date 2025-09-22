using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[ApiController]
[Authorize]
[Route("api/client/servers/{serverId:int}")]
public class PowerController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly ServerService ServerService;
    private readonly ServerAuthorizeService AuthorizeService;

    public PowerController(
        DatabaseRepository<Server> serverRepository,
        ServerService serverService,
        ServerAuthorizeService authorizeService
    )
    {
        ServerRepository = serverRepository;
        ServerService = serverService;
        AuthorizeService = authorizeService;
    }

    [HttpPost("start")]
    [Authorize]
    public async Task<ActionResult> StartAsync([FromRoute] int serverId)
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.StartAsync(server.Value);
        return NoContent();
    }

    [HttpPost("stop")]
    [Authorize]
    public async Task<ActionResult> StopAsync([FromRoute] int serverId)
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.StopAsync(server.Value);
        return NoContent();
    }

    [HttpPost("kill")]
    [Authorize]
    public async Task<ActionResult> KillAsync([FromRoute] int serverId)
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.KillAsync(server.Value);
        return NoContent();
    }

    private async Task<ActionResult<Server>> GetServerByIdAsync(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.AuthorizeAsync(
            User, server,
            ServerPermissionConstants.Power,
            ServerPermissionLevel.ReadWrite
        );

        if (!authorizeResult.Succeeded)
        {
            return Problem(
                authorizeResult.Message ?? "No permission for the requested resource",
                statusCode: 403
            );
        }

        return server;
    }
}