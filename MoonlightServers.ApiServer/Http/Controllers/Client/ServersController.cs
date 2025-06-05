using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Extensions;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Responses.Client.Servers;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Allocations;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<ServerShare> ShareRepository;
    private readonly NodeService NodeService;
    private readonly ServerAuthorizeService AuthorizeService;

    public ServersController(
        DatabaseRepository<Server> serverRepository,
        NodeService nodeService,
        ServerService serverService,
        ServerAuthorizeService authorizeService,
        DatabaseRepository<ServerShare> shareRepository
    )
    {
        ServerRepository = serverRepository;
        NodeService = nodeService;
        ServerService = serverService;
        AuthorizeService = authorizeService;
        ShareRepository = shareRepository;
    }

    [HttpGet]
    public async Task<PagedData<ServerDetailResponse>> GetAll([FromQuery] int page, [FromQuery] int pageSize)
    {
        var userIdClaim = User.FindFirstValue("userId");

        if (string.IsNullOrEmpty(userIdClaim))
            throw new HttpApiException("Only users are able to use this endpoint", 400);

        var userId = int.Parse(userIdClaim);

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
            Cpu = x.Cpu,
            Memory = x.Memory,
            Disk = x.Disk,
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

    [HttpGet("shared")]
    public async Task<PagedData<ServerDetailResponse>> GetAllShared([FromQuery] int page, [FromQuery] int pageSize)
    {
        var userIdClaim = User.FindFirstValue("userId");

        if (string.IsNullOrEmpty(userIdClaim))
            throw new HttpApiException("Only users are able to use this endpoint", 400);

        var userId = int.Parse(userIdClaim);

        var query = ShareRepository
            .Get()
            .Include(x => x.Server)
            .Where(x => x.UserId == userId)
            .Select(x => x.Server);

        var count = await query.CountAsync();
        var items = await query.Skip(page * pageSize).Take(pageSize).ToArrayAsync();

        var mappedItems = items.Select(x => new ServerDetailResponse()
        {
            Id = x.Id,
            Name = x.Name,
            NodeName = x.Node.Name,
            StarName = x.Star.Name,
            Cpu = x.Cpu,
            Memory = x.Memory,
            Disk = x.Disk,
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
    public async Task<ServerDetailResponse> Get([FromRoute] int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        if (!await AuthorizeService.Authorize(User, server))
            throw new HttpApiException("No server with this id found", 404);

        return new ServerDetailResponse()
        {
            Id = server.Id,
            Name = server.Name,
            NodeName = server.Node.Name,
            StarName = server.Star.Name,
            Cpu = server.Cpu,
            Memory = server.Memory,
            Disk = server.Disk,
            Allocations = server.Allocations.Select(y => new AllocationDetailResponse()
            {
                Id = y.Id,
                Port = y.Port,
                IpAddress = y.IpAddress
            }).ToArray()
        };
    }

    [HttpGet("{serverId:int}/status")]
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
    public async Task<ServerWebSocketResponse> GetWebSocket([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId,
            permission => permission is { Name: "console", Type: >= ServerPermissionType.Read }
        );

        // TODO: Handle transparent node proxy

        var accessToken = NodeService.CreateAccessToken(server.Node, parameters =>
        {
            parameters.Add("type", "websocket");
            parameters.Add("serverId", server.Id);
        }, TimeSpan.FromMinutes(15)); // TODO: Configurable

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
    public async Task<ServerLogsResponse> GetLogs([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId,
            permission => permission is { Name: "console", Type: >= ServerPermissionType.Read }
        );

        var logs = await ServerService.GetLogs(server);

        return new ServerLogsResponse()
        {
            Messages = logs.Messages
        };
    }

    [HttpGet("{serverId:int}/stats")]
    public async Task<ServerStatsResponse> GetStats([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId
        );

        var stats = await ServerService.GetStats(server);

        return new ServerStatsResponse()
        {
            CpuUsage = stats.CpuUsage,
            MemoryUsage = stats.MemoryUsage,
            NetworkRead = stats.NetworkRead,
            NetworkWrite = stats.NetworkWrite,
            IoRead = stats.IoRead,
            IoWrite = stats.IoWrite
        };
    }

    private async Task<Server> GetServerById(int serverId, Func<ServerSharePermission, bool>? filter = null)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        if (!await AuthorizeService.Authorize(User, server, filter))
            throw new HttpApiException("No server with this id found", 404);

        return server;
    }
}