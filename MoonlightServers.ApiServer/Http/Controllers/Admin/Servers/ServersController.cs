using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Servers;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.Servers;
using MoonlightServers.Shared.Http.Responses.Admin.ServerVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Servers;

[ApiController]
[Route("api/admin/servers")]
public class ServersController : Controller
{
    private readonly CrudHelper<Server, ServerDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly DatabaseRepository<Allocation> AllocationRepository;
    private readonly DatabaseRepository<ServerVariable> VariableRepository;
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;

    public ServersController(
        CrudHelper<Server, ServerDetailResponse> crudHelper,
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<Node> nodeRepository,
        DatabaseRepository<Allocation> allocationRepository,
        DatabaseRepository<ServerVariable> variableRepository,
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<User> userRepository)
    {
        CrudHelper = crudHelper;
        StarRepository = starRepository;
        NodeRepository = nodeRepository;
        AllocationRepository = allocationRepository;
        VariableRepository = variableRepository;
        ServerRepository = serverRepository;
        UserRepository = userRepository;

        CrudHelper.QueryModifier = servers => servers
            .Include(x => x.Node)
            .Include(x => x.Allocations)
            .Include(x => x.Variables)
            .Include(x => x.Star);

        CrudHelper.LateMapper = (server, response) =>
        {
            response.NodeId = server.Node.Id;
            response.StarId = server.Star.Id;
            response.AllocationIds = server.Allocations.Select(x => x.Id).ToArray();

            return response;
        };
    }

    [HttpGet]
    [RequirePermission("admin.servers.get")]
    public async Task<IPagedData<ServerDetailResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("admin.servers.get")]
    public async Task<ServerDetailResponse> GetSingle([FromRoute] int id)
    {
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost]
    [RequirePermission("admin.servers.create")]
    public async Task<ServerDetailResponse> Create([FromBody] CreateServerRequest request)
    {
        // Construct model
        var server = Mapper.Map<Server>(request);

        // Check if owner user exist
        if (UserRepository.Get().All(x => x.Id != request.OwnerId))
            throw new HttpApiException("No user with this id found", 400);

        var star = StarRepository
            .Get()
            .Include(x => x.Variables)
            .Include(x => x.DockerImages)
            .FirstOrDefault(x => x.Id == request.StarId);

        if (star == null)
            throw new HttpApiException("No star with this id found", 400);

        var node = NodeRepository
            .Get()
            .FirstOrDefault(x => x.Id == request.NodeId);

        if (node == null)
            throw new HttpApiException("No node with this id found", 400);

        var allocations = new List<Allocation>();

        // Fetch specified allocations from the request
        foreach (var allocationId in request.AllocationIds)
        {
            var allocation = AllocationRepository
                .Get()
                .Where(x => x.Server == null)
                .Where(x => x.Node.Id == node.Id)
                .FirstOrDefault(x => x.Id == allocationId);

            if (allocation == null)
                continue;

            allocations.Add(allocation);
        }

        // Check if the specified allocations are enough for the star
        if (allocations.Count < star.RequiredAllocations)
        {
            var amountRequiredToSatisfy = star.RequiredAllocations - allocations.Count;

            var freeAllocations = AllocationRepository
                .Get()
                .Where(x => x.Server == null)
                .Where(x => x.Node.Id == node.Id)
                .Take(amountRequiredToSatisfy);

            allocations.AddRange(freeAllocations);

            if (allocations.Count < star.RequiredAllocations)
            {
                throw new HttpApiException(
                    $"Unable to find enough free allocations. Found: {allocations.Count}, Required: {star.RequiredAllocations}",
                    400
                );
            }
        }

        // Set allocations
        server.Allocations = allocations;

        // Variables
        foreach (var variable in star.Variables)
        {
            var serverVar = new ServerVariable()
            {
                Key = variable.Key,
                Value = request.Variables.TryGetValue(variable.Key, out var value)
                    ? value
                    : variable.DefaultValue
            };

            server.Variables.Add(serverVar);
        }

        // Set relations
        server.Node = node;
        server.Star = star;

        // TODO: Call node

        var finalServer = ServerRepository.Add(server);

        return CrudHelper.MapToResult(finalServer);
    }

    [HttpPatch("{id:int}")]
    public async Task<ServerDetailResponse> Update([FromRoute] int id, [FromBody] UpdateServerRequest request)
    {
        var server = await CrudHelper.GetSingleModel(id);

        server = Mapper.Map(server, request);

        var allocations = new List<Allocation>();

        // Fetch specified allocations from the request
        foreach (var allocationId in request.AllocationIds)
        {
            var allocation = AllocationRepository
                .Get()
                .Where(x => x.Server == null || x.Server.Id == server.Id)
                .Where(x => x.Node.Id == server.Node.Id)
                .FirstOrDefault(x => x.Id == allocationId);
            
            // ^ This loads the allocations specified in the request.
            // Valid allocations are either free ones or ones which are already allocated to this server

            if (allocation == null)
                continue;

            allocations.Add(allocation);
        }

        // Check if the specified allocations are enough for the star
        if (allocations.Count < server.Star.RequiredAllocations)
        {
            throw new HttpApiException(
                $"You need to specify at least {server.Star.RequiredAllocations} allocation(s)",
                400
            );
        }

        // Set allocations
        server.Allocations = allocations;
        
        ServerRepository.Update(server);

        return CrudHelper.MapToResult(server);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete([FromRoute] int id)
    {
        await CrudHelper.Delete(id);
    }
}