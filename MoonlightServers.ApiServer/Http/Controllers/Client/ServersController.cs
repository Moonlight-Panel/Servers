using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Models;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Extensions;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.ApiServer.Models;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers;
using MoonlightServers.Shared.Http.Responses.Client.Servers;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Allocations;
using MoonlightServers.Shared.Models;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class ServersController : Controller
{
    private readonly ServerService ServerService;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<ServerShare> ShareRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly NodeService NodeService;
    private readonly ServerAuthorizeService AuthorizeService;

    public ServersController(
        DatabaseRepository<Server> serverRepository,
        NodeService nodeService,
        ServerService serverService,
        ServerAuthorizeService authorizeService,
        DatabaseRepository<ServerShare> shareRepository,
        DatabaseRepository<User> userRepository
    )
    {
        ServerRepository = serverRepository;
        NodeService = nodeService;
        ServerService = serverService;
        AuthorizeService = authorizeService;
        ShareRepository = shareRepository;
        UserRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedData<ServerDetailResponse>>> GetAll([FromQuery] PagedOptions options)
    {
        var userIdClaim = User.FindFirstValue("UserId");

        if (string.IsNullOrEmpty(userIdClaim))
            return Problem("Only users are able to use this endpoint", statusCode: 400);

        var userId = int.Parse(userIdClaim);

        var query = ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .Where(x => x.OwnerId == userId);

        var count = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ToArrayAsync();

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
            CurrentPage = options.Page,
            TotalItems = count,
            PageSize = options.PageSize,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("shared")]
    public async Task<ActionResult<PagedData<ServerDetailResponse>>> GetAllShared([FromQuery] PagedOptions options)
    {
        var userIdClaim = User.FindFirstValue("UserId");

        if (string.IsNullOrEmpty(userIdClaim))
            return Problem("Only users are able to use this endpoint", statusCode: 400);

        var userId = int.Parse(userIdClaim);

        var query = ShareRepository
            .Get()
            .Include(x => x.Server)
            .ThenInclude(x => x.Node)
            .Include(x => x.Server)
            .ThenInclude(x => x.Star)
            .Include(x => x.Server)
            .ThenInclude(x => x.Allocations)
            .Where(x => x.UserId == userId);

        var count = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .ToArrayAsync();

        var ownerIds = items
            .Select(x => x.Server.OwnerId)
            .Distinct()
            .ToArray();

        var owners = await UserRepository
            .Get()
            .Where(x => ownerIds.Contains(x.Id))
            .ToArrayAsync();

        var mappedItems = items.Select(x => new ServerDetailResponse()
        {
            Id = x.Server.Id,
            Name = x.Server.Name,
            NodeName = x.Server.Node.Name,
            StarName = x.Server.Star.Name,
            Cpu = x.Server.Cpu,
            Memory = x.Server.Memory,
            Disk = x.Server.Disk,
            Allocations = x.Server.Allocations.Select(y => new AllocationDetailResponse()
            {
                Id = y.Id,
                Port = y.Port,
                IpAddress = y.IpAddress
            }).ToArray(),
            Share = new()
            {
                SharedBy = owners.First(y => y.Id == x.Server.OwnerId).Username,
                Permissions = ShareMapper.MapToPermissionLevels(x.Content)
            }
        }).ToArray();

        return new PagedData<ServerDetailResponse>()
        {
            Items = mappedItems,
            CurrentPage = options.Page,
            TotalItems = count,
            PageSize = options.PageSize,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("{serverId:int}")]
    public async Task<ActionResult<ServerDetailResponse>> Get([FromRoute] int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Allocations)
            .Include(x => x.Star)
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizationResult = await AuthorizeService.Authorize(
            User,
            server,
            String.Empty,
            ServerPermissionLevel.None
        );

        if (!authorizationResult.Succeeded)
        {
            return Problem(
                authorizationResult.Message ?? "No server with this id found",
                statusCode: 404
            );
        }

        // Create mapped response
        var response = new ServerDetailResponse()
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

        // Handle requests on shared servers
        if (authorizationResult.Share != null)
        {
            var owner = await UserRepository
                .Get()
                .FirstAsync(x => x.Id == server.OwnerId);

            response.Share = new()
            {
                SharedBy = owner.Username,
                Permissions = ShareMapper.MapToPermissionLevels(authorizationResult.Share.Content)
            };
        }

        return response;
    }

    [HttpGet("{serverId:int}/status")]
    public async Task<ActionResult<ServerStatusResponse>> GetStatus([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId,
            ServerPermissionConstants.Console,
            ServerPermissionLevel.None
        );
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var status = await ServerService.GetStatus(server.Value);

        return new ServerStatusResponse()
        {
            State = status.State.ToServerPowerState()
        };
    }

    [HttpGet("{serverId:int}/ws")]
    public async Task<ActionResult<ServerWebSocketResponse>> GetWebSocket([FromRoute] int serverId)
    {
        var serverResult = await GetServerById(
            serverId,
            ServerPermissionConstants.Console,
            ServerPermissionLevel.Read
        );
        
        if (serverResult.Value == null)
            return serverResult.Result ?? Problem("Unable to retrieve server");

        var server = serverResult.Value;

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
    public async Task<ActionResult<ServerLogsResponse>> GetLogs([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId,
            ServerPermissionConstants.Console,
            ServerPermissionLevel.Read
        );
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var logs = await ServerService.GetLogs(server.Value);

        return new ServerLogsResponse()
        {
            Messages = logs.Messages
        };
    }

    [HttpGet("{serverId:int}/stats")]
    public async Task<ActionResult<ServerStatsResponse>> GetStats([FromRoute] int serverId)
    {
        var server = await GetServerById(
            serverId,
            ServerPermissionConstants.Console,
            ServerPermissionLevel.Read
        );
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var stats = await ServerService.GetStats(server.Value);

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

    [HttpPost("{serverId:int}/command")]
    public async Task<ActionResult> Command([FromRoute] int serverId, [FromBody] ServerCommandRequest request)
    {
        var server = await GetServerById(
            serverId,
            ServerPermissionConstants.Console,
            ServerPermissionLevel.ReadWrite
        );

        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        await ServerService.RunCommand(server.Value, request.Command);

        return NoContent();
    }

    private async Task<ActionResult<Server>> GetServerById(int serverId, string permissionId, ServerPermissionLevel level)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.Authorize(User, server, permissionId, level);

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