using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extensions;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Extensions;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses.User.Allocations;
using MoonlightServers.Shared.Http.Responses.Users.Servers;

namespace MoonlightServers.ApiServer.Http.Controllers.Users;

[ApiController]
[Route("api/servers")]
public class ServersController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly NodeService NodeService;

    public ServersController(DatabaseRepository<Server> serverRepository, NodeService nodeService)
    {
        ServerRepository = serverRepository;
        NodeService = nodeService;
    }

    [HttpGet]
    [RequirePermission("meta.authenticated")]
    public async Task<PagedData<ServerDetailResponse>> GetAll([FromQuery] int page, [FromQuery] int pageSize)
    {
        var user = User.AsIdentity<User>();

        var query = ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .Where(x => x.OwnerId == user.Id);

        var count = await query.CountAsync();
        var items = await query.Skip(page * pageSize).Take(pageSize).ToArrayAsync();

        var mappedItems = items.Select(x => new ServerDetailResponse()
        {
            Id = x.Id,
            Name = x.Name,
            NodeName = x.Node.Name,
            StarName = x.Star.Name,
            Allocations = x.Allocations.Select(y => new AllocationDetailResponse()
            {
                Id = y.Id,
                Port = y.Port,
                IpAddress = y.IpAddress
            }).ToArray()
        }).ToArray();

        return new PagedData<ServerDetailResponse>()
        {
            Items = mappedItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{serverId:int}")]
    [RequirePermission("meta.authenticated")]
    public async Task<ServerDetailResponse> Get([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(
            serverId,
            query =>
                query
                    .Include(x => x.Allocations)
                    .Include(x => x.Star)
        );

        return new ServerDetailResponse()
        {
            Id = server.Id,
            Name = server.Name,
            NodeName = server.Node.Name,
            StarName = server.Star.Name,
            Allocations = server.Allocations.Select(y => new AllocationDetailResponse()
            {
                Id = y.Id,
                Port = y.Port,
                IpAddress = y.IpAddress
            }).ToArray()
        };
    }

    [HttpGet("{serverId:int}/status")]
    [RequirePermission("meta.authenticated")]
    public async Task<ServerStatusResponse> GetStatus([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            var data = await apiClient.GetJson<DaemonShared.DaemonSide.Http.Responses.Servers.ServerStatusResponse>(
                $"api/servers/{server.Id}/status"
            );

            return new ServerStatusResponse()
            {
                PowerState = data.State.ToServerPowerState()
            };
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    private async Task<Server> GetServerWithPermCheck(int serverId,
        Func<IQueryable<Server>, IQueryable<Server>>? queryModifier = null)
    {
        var user = User.AsIdentity<User>();

        var query = ServerRepository
            .Get()
            .Include(x => x.Node) as IQueryable<Server>;

        if (queryModifier != null)
            query = queryModifier.Invoke(query);

        var server = await query
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        if (server.OwnerId == user.Id) // The current user is the owner
            return server;

        if (User.HasPermission("admin.servers.get")) // The current user is an admin
            return server;

        throw new HttpApiException("No server with this id found", 404);
    }
}