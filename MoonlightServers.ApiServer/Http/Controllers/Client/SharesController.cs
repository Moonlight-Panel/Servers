using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Common;
using MoonCore.Extended.Abstractions;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Shares;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Shares;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers/{serverId:int}/shares")]
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

    [HttpGet]
    public async Task<ActionResult<CountedData<ServerShareResponse>>> GetAllAsync(
        [FromRoute] int serverId,
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var query = ShareRepository
            .Get()
            .Where(x => x.Server.Id == server.Value.Id);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .ToArrayAsync();

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
            Permissions = ShareMapper.MapToPermissionLevels(x.Content)
        }).ToArray();

        return new CountedData<ServerShareResponse>()
        {
            Items = mappedItems,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServerShareResponse>> GetAsync(
        [FromRoute] int serverId,
        [FromRoute] int id
    )
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Value.Id && x.Id == id);

        if (share == null)
            return Problem("A share with that id cannot be found", statusCode: 404);

        var user = await UserRepository
            .Get()
            .FirstAsync(x => x.Id == share.UserId);

        var mappedItem = new ServerShareResponse()
        {
            Id = share.Id,
            Username = user.Username,
            Permissions = ShareMapper.MapToPermissionLevels(share.Content)
        };

        return mappedItem;
    }

    [HttpPost]
    public async Task<ActionResult<ServerShareResponse>> CreateAsync(
        [FromRoute] int serverId,
        [FromBody] CreateShareRequest request
    )
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Username == request.Username);

        if (user == null)
            return Problem("A user with that username could not be found", statusCode: 400);

        var share = new ServerShare()
        {
            Server = server.Value,
            Content = ShareMapper.MapToServerShareContent(request.Permissions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        var finalShare = await ShareRepository.AddAsync(share);

        var mappedItem = new ServerShareResponse()
        {
            Id = finalShare.Id,
            Username = user.Username,
            Permissions = ShareMapper.MapToPermissionLevels(finalShare.Content)
        };

        return mappedItem;
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<ServerShareResponse>> UpdateAsync(
        [FromRoute] int serverId,
        [FromRoute] int id,
        [FromBody] UpdateShareRequest request
    )
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Value.Id && x.Id == id);

        if (share == null)
            return Problem("A share with that id cannot be found", statusCode: 404);

        share.Content = ShareMapper.MapToServerShareContent(request.Permissions);

        share.UpdatedAt = DateTime.UtcNow;

        await ShareRepository.UpdateAsync(share);

        var user = await UserRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == share.UserId);

        if (user == null)
            return Problem("A user with that id could not be found", statusCode: 400);

        var mappedItem = new ServerShareResponse()
        {
            Id = share.Id,
            Username = user.Username,
            Permissions = ShareMapper.MapToPermissionLevels(share.Content)
        };

        return mappedItem;
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] int serverId,
        [FromRoute] int id
    )
    {
        var server = await GetServerByIdAsync(serverId);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var share = await ShareRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Server.Id == server.Value.Id && x.Id == id);

        if (share == null)
            return Problem("A share with that id cannot be found", statusCode: 404);

        await ShareRepository.RemoveAsync(share);
        return NoContent();
    }

    private async Task<ActionResult<Server>> GetServerByIdAsync(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.AuthorizeAsync(
            User, server,
            ServerPermissionConstants.Shares,
            ServerPermissionLevel.ReadWrite
        );

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