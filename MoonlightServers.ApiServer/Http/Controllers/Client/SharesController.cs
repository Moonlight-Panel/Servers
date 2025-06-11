using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Shares;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class SharesController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<ServerShare> ShareRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ServerAuthorizeService AuthorizeService;

    public SharesController(
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<ServerShare> shareRepository,
        DatabaseRepository<User> userRepository,
        ServerAuthorizeService authorizeService
    )
    {
        ServerRepository = serverRepository;
        ShareRepository = shareRepository;
        UserRepository = userRepository;
        AuthorizeService = authorizeService;
    }

    [HttpGet("{serverId:int}/shares")]
    public async Task<PagedData<ServerShareResponse>> GetAll(
        [FromRoute] int serverId,
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var server = await GetServerById(serverId);

        var query = ShareRepository
            .Get()
            .Where(x => x.Server.Id == server.Id);

        var count = await query.CountAsync();
        var items = await query.Skip(page * pageSize).Take(pageSize).ToArrayAsync();

        var userIds = items
            .Select(x => x.UserId)
            .Distinct()
            .ToArray();

        var users = await UserRepository
            .Get()
            .Where(x => userIds.Contains(x.Id))
            .ToArrayAsync();

        var mappedItems = items.Select(x => new ServerShareResponse()
        {
            Id = x.Id,
            Username = users.First(y => y.Id == x.UserId).Username,
            Permissions = x.Content.Permissions.ToArray()
        }).ToArray();

        return new PagedData<ServerShareResponse>()
        {
            Items = mappedItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{serverId:int}/shares/{id:int}")]
    public async Task<ServerShareResponse> Get(
        [FromRoute] int serverId,
        [FromRoute] int id
    )
    {
        var server = await GetServerById(serverId);

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.Id == id);

        if (share == null)
            throw new HttpApiException("A share with that id cannot be found", 404);

        var user = await UserRepository
            .Get()
            .FirstAsync(x => x.Id == share.UserId);

        var mappedItem = new ServerShareResponse()
        {
            Id = share.Id,
            Username = user.Username,
            Permissions = share.Content.Permissions.ToArray()
        };

        return mappedItem;
    }

    [HttpPost("{serverId:int}/shares")]
    public async Task<ServerShareResponse> Create(
        [FromRoute] int serverId,
        [FromBody] CreateShareRequest request
    )
    {
        var server = await GetServerById(serverId);

        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Username == request.Username);

        if (user == null)
            throw new HttpApiException("A user with that username could not be found", 400);

        var share = new ServerShare()
        {
            Server = server,
            Content = new()
            {
                Permissions = request.Permissions
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        var finalShare = await ShareRepository.Add(share);

        var mappedItem = new ServerShareResponse()
        {
            Id = finalShare.Id,
            Username = user.Username,
            Permissions = finalShare.Content.Permissions.ToArray()
        };

        return mappedItem;
    }

    [HttpPatch("{serverId:int}/shares/{id:int}")]
    public async Task<ServerShareResponse> Update(
        [FromRoute] int serverId,
        [FromRoute] int id,
        [FromBody] UpdateShareRequest request
    )
    {
        var server = await GetServerById(serverId);

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.Id == id);

        if (share == null)
            throw new HttpApiException("A share with that id cannot be found", 404);

        share.Content.Permissions = request.Permissions;
        share.UpdatedAt = DateTime.UtcNow;

        await ShareRepository.Update(share);
        
        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == share.UserId);

        if (user == null)
            throw new HttpApiException("A user with that id could not be found", 400);
        
        var mappedItem = new ServerShareResponse()
        {
            Id = share.Id,
            Username = user.Username,
            Permissions = share.Content.Permissions.ToArray()
        };

        return mappedItem;
    }

    [HttpDelete("{serverId:int}/shares/{id:int}")]
    public async Task Delete(
        [FromRoute] int serverId,
        [FromRoute] int id
    )
    {
        var server = await GetServerById(serverId);

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Id && x.Id == id);

        if (share == null)
            throw new HttpApiException("A share with that id cannot be found", 404);

        await ShareRepository.Remove(share);
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
            permission => permission is { Name: "shares", Type: >= ServerPermissionType.ReadWrite }
        );

        if (!authorizeResult.Succeeded)
        {
            throw new HttpApiException(
                authorizeResult.Message ?? "No permission for the requested resource",
                403
            );
        }

        return server;
    }
}