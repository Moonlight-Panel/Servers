using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
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
    public async Task Install([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        await ServerService.Install(server);
    }
    
    private async Task<Server> GetServerById(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        if (!await AuthorizeService.Authorize(User, server, permission => permission is { Name: "settings", Type: ServerPermissionType.ReadWrite }))
            throw new HttpApiException("No server with this id found", 404);

        return server;
    }
}