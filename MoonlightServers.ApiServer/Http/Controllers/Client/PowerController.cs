using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[ApiController]
[Authorize]
[Route("api/client/servers")]
public class PowerController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ServerService ServerService;
    private readonly ServerAuthorizeService AuthorizeService;

    public PowerController(
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<User> userRepository,
        ServerService serverService,
        ServerAuthorizeService authorizeService
    )
    {
        ServerRepository = serverRepository;
        UserRepository = userRepository;
        ServerService = serverService;
        AuthorizeService = authorizeService;
    }

    [HttpPost("{serverId:int}/start")]
    [Authorize]
    public async Task Start([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        await ServerService.Start(server);
    }

    [HttpPost("{serverId:int}/stop")]
    [Authorize]
    public async Task Stop([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        await ServerService.Stop(server);
    }

    [HttpPost("{serverId:int}/kill")]
    [Authorize]
    public async Task Kill([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);
        await ServerService.Kill(server);
    }

    private async Task<Server> GetServerById(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var authorizeResult = await AuthorizeService.Authorize(
            User, server,
            permission => permission.Name == "power" && permission.Type >= ServerPermissionType.ReadWrite
        );

        if (!authorizeResult.Succeeded)
            throw new HttpApiException("No permission for the requested resource", 403);
        
        return server;
    }
}