using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class SettingsController : Controller
{
    private readonly ServerService ServerService;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly ServerAuthorizeService AuthorizeService;

    public SettingsController(
        ServerService serverService,
        DatabaseRepository<Server> serverRepository,
        ServerAuthorizeService authorizeService
    )
    {
        ServerService = serverService;
        ServerRepository = serverRepository;
        AuthorizeService = authorizeService;
    }

    [HttpPost("{serverId:int}/install")]
    [Authorize]
    public async Task<ActionResult> InstallAsync([FromRoute] int serverId)
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");
        
        await ServerService.InstallAsync(server.Value);
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
            ServerPermissionConstants.Settings,
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