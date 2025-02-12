using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.PermFilter;
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
    [Authorize]
    public async Task<PagedData<ServerDetailResponse>> GetAll([FromQuery] int page, [FromQuery] int pageSize)
    {
        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);

        var query = ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .Where(x => x.OwnerId == userId);

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
    [Authorize]
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
    [Authorize]
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
                State = data.State.ToServerPowerState()
            };
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }
    
    [HttpGet("{serverId:int}/ws")]
    [Authorize]
    public async Task<ServerWebSocketResponse> GetWebSocket([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        // TODO: Handle transparent node proxy

        var accessToken = NodeService.CreateAccessToken(server.Node, parameters =>
        {
            parameters.Add("type", "websocket");
            parameters.Add("serverId", server.Id);
        }, TimeSpan.FromMinutes(10));
        
        var url = "";

        if (server.Node.UseSsl)
            url += "https://";
        else
            url += "http://";

        url += $"{server.Node.Fqdn}:{server.Node.HttpPort}/api/servers/ws";

        return new ServerWebSocketResponse()
        {
            Target = url,
            AccessToken = accessToken
        };
    }
    
    [HttpGet("{serverId:int}/logs")]
    [Authorize]
    public async Task<ServerLogsResponse> GetLogs([FromRoute] int serverId)
    {
        var server = await GetServerWithPermCheck(serverId);

        var apiClient = await NodeService.CreateApiClient(server.Node);

        try
        {
            var data = await apiClient.GetJson<DaemonShared.DaemonSide.Http.Responses.Servers.ServerLogsResponse>(
                $"api/servers/{server.Id}/logs"
            );

            return new ServerLogsResponse()
            {
                Messages = data.Messages
            };
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
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

        if (User.HasPermission("admin.servers.get")) // The current user is an admin
            return server;

        throw new HttpApiException("No server with this id found", 404);
    }
}