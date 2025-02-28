using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[ApiController]
[Authorize]
[Route("api/client/servers")]
public class ServerPowerController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly NodeService NodeService;

    public ServerPowerController(DatabaseRepository<Server> serverRepository, NodeService nodeService)
    {
        ServerRepository = serverRepository;
        NodeService = nodeService;
    }

    [HttpPost("{serverId:int}/start")]
    [Authorize]
    public async Task Start([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        using var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            await apiClient.Post($"api/servers/{server.Id}/start");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    [HttpPost("{serverId:int}/stop")]
    [Authorize]
    public async Task Stop([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        using var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            await apiClient.Post($"api/servers/{server.Id}/stop");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    [HttpPost("{serverId:int}/kill")]
    [Authorize]
    public async Task Kill([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        using var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            await apiClient.Post($"api/servers/{server.Id}/kill");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    [HttpPost("{serverId:int}/install")]
    [Authorize]
    public async Task Install([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        using var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            await apiClient.Post($"api/servers/{server.Id}/install");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    private async Task<Server> GetServerWithPermCheck(int serverId,
        Func<IQueryable<Server>, IQueryable<Server>>? queryModifier = null)
    {
        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);

        var query = ServerRepository
            .Get()
            .Include(x => x.Node) as IQueryable<Server>;

        if (queryModifier != null)
            query = queryModifier.Invoke(query);

        var server = await query
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        if (server.OwnerId == userId) // The current user is the owner
            return server;

        var permissions = User.Claims.First(x => x.Type == "permissions").Value.Split(";", StringSplitOptions.RemoveEmptyEntries);
        
        if (PermissionHelper.HasPermission(permissions, "admin.servers.get")) // The current user is an admin
            return server;

        throw new HttpApiException("No server with this id found", 404);
    }
}