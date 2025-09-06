using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.Database.Entities;
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
    public async Task<ActionResult> Start([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.Start(server.Value);
        return NoContent();
    }

    [HttpPost("stop")]
    [Authorize]
    public async Task<ActionResult> Stop([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.Stop(server.Value);
        return NoContent();
    }

    [HttpPost("kill")]
    [Authorize]
    public async Task<ActionResult> Kill([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.Kill(server.Value);
        return NoContent();
    }

    private async Task<ActionResult<Server>> GetServerById(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.Authorize(
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