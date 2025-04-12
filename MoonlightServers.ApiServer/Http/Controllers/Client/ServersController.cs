using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Extensions;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses.User.Allocations;
using MoonlightServers.Shared.Http.Responses.Users.Servers;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[ApiController]
[Route("api/client/servers")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly NodeService NodeService;

    public ServersController(DatabaseRepository<Server> serverRepository, NodeService nodeService, ServerService serverService, DatabaseRepository<User> userRepository)
    {
        ServerRepository = serverRepository;
        NodeService = nodeService;
        ServerService = serverService;
        UserRepository = userRepository;
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
        var server = await ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);
        
        if(server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);
        var user = await UserRepository.Get().FirstAsync(x => x.Id == userId);
        
        if(!ServerService.IsAllowedToAccess(user, server))
            throw new HttpApiException("No server with this id found", 404);

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
        var server = await GetServerById(serverId);
        var status = await ServerService.GetStatus(server);

        return new ServerStatusResponse()
        {
            State = status.State.ToServerPowerState()
        };
    }
    
    [HttpGet("{serverId:int}/ws")]
    [Authorize]
    public async Task<ServerWebSocketResponse> GetWebSocket([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);

        // TODO: Handle transparent node proxy

        var accessToken = NodeService.CreateAccessToken(server.Node, parameters =>
        {
            parameters.Add("type", "websocket");
            parameters.Add("serverId", server.Id);
        }, TimeSpan.FromSeconds(30));
        
        var url = "";

        url += server.Node.UseSsl ? "https://" : "http://";
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
        var server = await GetServerById(serverId);

        var logs = await ServerService.GetLogs(server);
        
        return new ServerLogsResponse()
        {
            Messages = logs.Messages
        };
    }

    private async Task<Server> GetServerById(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);
        
        if(server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);
        var user = await UserRepository.Get().FirstAsync(x => x.Id == userId);
        
        if(!ServerService.IsAllowedToAccess(user, server))
            throw new HttpApiException("No server with this id found", 404);

        return server;
    }
}