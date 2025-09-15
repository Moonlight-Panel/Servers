using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Extended.Models;
using MoonCore.Helpers;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Requests.Admin.Servers;
using MoonlightServers.Shared.Http.Responses.Admin.Servers;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Servers;

[ApiController]
[Route("api/admin/servers")]
public class ServersController : Controller
{
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly DatabaseRepository<Allocation> AllocationRepository;
    private readonly DatabaseRepository<ServerVariable> VariableRepository;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ILogger<ServersController> Logger;
    private readonly ServerService ServerService;

    public ServersController(
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<Node> nodeRepository,
        DatabaseRepository<Allocation> allocationRepository,
        DatabaseRepository<ServerVariable> variableRepository,
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<User> userRepository,
        ILogger<ServersController> logger,
        ServerService serverService
    )
    {
        StarRepository = starRepository;
        NodeRepository = nodeRepository;
        AllocationRepository = allocationRepository;
        VariableRepository = variableRepository;
        ServerRepository = serverRepository;
        UserRepository = userRepository;
        ServerService = serverService;
        Logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.read")]
    public async Task<ActionResult<IPagedData<ServerResponse>>> Get([FromQuery] PagedOptions options)
    {
        var count = await ServerRepository.Get().CountAsync();

        var servers = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .Include(x => x.Allocations)
            .Include(x => x.Variables)
            .Include(x => x.Star)
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new PagedData<ServerResponse>()
        {
            Items = servers,
            CurrentPage = options.Page,
            PageSize = options.PageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.read")]
    public async Task<ActionResult<ServerResponse>> GetSingle([FromRoute] int id)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .Include(x => x.Allocations)
            .Include(x => x.Variables)
            .Include(x => x.Star)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .ProjectToAdminResponse()
            .FirstOrDefaultAsync();

        if (server == null)
            return Problem("No server with that id found", statusCode: 404);

        return server;
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.write")]
    public async Task<ActionResult<ServerResponse>> Create([FromBody] CreateServerRequest request)
    {
        // Check if owner user exist
        if (UserRepository.Get().All(x => x.Id != request.OwnerId))
            return Problem("No user with this id found", statusCode: 400);

        // Check if the star exists
        var star = await StarRepository
            .Get()
            .Include(x => x.Variables)
            .Include(x => x.DockerImages)
            .FirstOrDefaultAsync(x => x.Id == request.StarId);

        if (star == null)
            return Problem("No star with this id found", statusCode: 400);

        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == request.NodeId);

        if (node == null)
            return Problem("No node with this id found", statusCode: 400);

        var allocations = new List<Allocation>();

        // Fetch specified allocations from the request
        foreach (var allocationId in request.AllocationIds)
        {
            var allocation = await AllocationRepository
                .Get()
                .Where(x => x.Server == null)
                .Where(x => x.Node.Id == node.Id)
                .FirstOrDefaultAsync(x => x.Id == allocationId);

            if (allocation == null)
                continue;

            allocations.Add(allocation);
        }

        // Check if the specified allocations are enough for the star
        if (allocations.Count < star.RequiredAllocations)
        {
            var amountRequiredToSatisfy = star.RequiredAllocations - allocations.Count;

            var freeAllocations = await AllocationRepository
                .Get()
                .Where(x => x.Server == null)
                .Where(x => x.Node.Id == node.Id)
                .Take(amountRequiredToSatisfy)
                .ToArrayAsync();

            allocations.AddRange(freeAllocations);

            if (allocations.Count < star.RequiredAllocations)
            {
                return Problem(
                    $"Unable to find enough free allocations. Found: {allocations.Count}, Required: {star.RequiredAllocations}",
                    statusCode: 400
                );
            }
        }

        var server = ServerMapper.ToServer(request);

        // Set allocations
        server.Allocations = allocations;

        // Variables
        foreach (var variable in star.Variables)
        {
            var requestVar = request.Variables.FirstOrDefault(x => x.Key == variable.Key);

            var serverVar = new ServerVariable()
            {
                Key = variable.Key,
                Value = requestVar != null
                    ? requestVar.Value
                    : variable.DefaultValue
            };

            server.Variables.Add(serverVar);
        }

        // Set relations
        server.Node = node;
        server.Star = star;

        var finalServer = await ServerRepository.Add(server);

        try
        {
            await ServerService.Sync(finalServer);
        }
        catch (Exception e)
        {
            Logger.LogError("Unable to sync server to node the server is assigned to: {e}", e);

            // We are deleting the server from the database after the creation has failed
            // to ensure we won't have a bugged server in the database which doesn't exist on the node
            await ServerRepository.Remove(finalServer);

            throw;
        }

        return ServerMapper.ToAdminServerResponse(finalServer);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.write")]
    public async Task<ActionResult<ServerResponse>> Update([FromRoute] int id, [FromBody] UpdateServerRequest request)
    {
        //TODO: Handle shrinking virtual disk

        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .Include(x => x.Allocations)
            .Include(x => x.Variables)
            .Include(x => x.Star)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (server == null)
            return Problem("No server with that id found", statusCode: 404);

        ServerMapper.Merge(request, server);

        var allocations = new List<Allocation>();

        // Fetch specified allocations from the request
        foreach (var allocationId in request.AllocationIds)
        {
            var allocation = await AllocationRepository
                .Get()
                .Where(x => x.Server == null || x.Server.Id == server.Id)
                .Where(x => x.Node.Id == server.Node.Id)
                .FirstOrDefaultAsync(x => x.Id == allocationId);

            // ^ This loads the allocations specified in the request.
            // Valid allocations are either free ones or ones which are already allocated to this server

            if (allocation == null)
                continue;

            allocations.Add(allocation);
        }

        // Check if the specified allocations are enough for the star
        if (allocations.Count < server.Star.RequiredAllocations)
        {
            return Problem(
                $"You need to specify at least {server.Star.RequiredAllocations} allocation(s)",
                statusCode: 400
            );
        }

        // Set allocations
        server.Allocations = allocations;

        // Process variables
        foreach (var variable in request.Variables)
        {
            // Search server variable associated to the variable in the request
            var serverVar = server.Variables
                .FirstOrDefault(x => x.Key == variable.Key);

            if (serverVar == null)
                continue;

            // Update value
            serverVar.Value = variable.Value;
        }

        await ServerRepository.Update(server);

        // Notify the node about the changes
        await ServerService.Sync(server);

        return ServerMapper.ToAdminServerResponse(server);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete([FromRoute] int id, [FromQuery] bool force = false)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Node)
            .Include(x => x.Star)
            .Include(x => x.Variables)
            .Include(x => x.Backups)
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (server == null)
            return Problem("No server with that id found", statusCode: 404);

        server.Variables.Clear();
        server.Backups.Clear();
        server.Allocations.Clear();

        try
        {
            // If the sync fails on the node and we aren't forcing the deletion,
            // we don't want to delete it from the database yet
            await ServerService.SyncDelete(server);
        }
        catch (Exception e)
        {
            if (force)
            {
                Logger.LogWarning(
                    "An error occured while syncing deletion of a server to the node. Continuing anyways. Error: {e}",
                    e
                );
            }
            else
                throw;
        }

        await ServerRepository.Remove(server);
        return NoContent();
    }
}