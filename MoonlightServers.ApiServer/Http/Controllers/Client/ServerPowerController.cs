using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[ApiController]
[Authorize]
[Route("api/client/servers")]
public class ServerPowerController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ServerService ServerService;

    public ServerPowerController(
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<User> userRepository,
        ServerService serverService
    )
    {
        ServerRepository = serverRepository;
        UserRepository = userRepository;
        ServerService = serverService;
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

        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);
        var user = await UserRepository.Get().FirstAsync(x => x.Id == userId);

        if (!ServerService.IsAllowedToAccess(user, server))
            throw new HttpApiException("No server with this id found", 404);

        return server;
    }
}