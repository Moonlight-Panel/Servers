using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes/{nodeId:int}/allocations")]
public class NodeAllocationsController : Controller
{
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly DatabaseRepository<Allocation> AllocationRepository;

    public NodeAllocationsController(
        DatabaseRepository<Node> nodeRepository,
        DatabaseRepository<Allocation> allocationRepository
    )
    {
        NodeRepository = nodeRepository;
        AllocationRepository = allocationRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<CountedData<NodeAllocationResponse>>> GetAsync(
        [FromRoute] int nodeId,
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var totalCount = await AllocationRepository
            .Get()
            .CountAsync(x => x.Node.Id == nodeId);

        var allocations = await AllocationRepository
            .Get()
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .Where(x => x.Node.Id == nodeId)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<NodeAllocationResponse>()
        {
            Items = allocations,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<NodeAllocationResponse>> GetSingleAsync([FromRoute] int nodeId, [FromRoute] int id)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            return Problem("No allocation with that id found", statusCode: 400);

        return allocation;
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<ActionResult<NodeAllocationResponse>> CreateAsync(
        [FromRoute] int nodeId,
        [FromBody] CreateNodeAllocationRequest request
    )
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (node == null)
            return Problem("No node with that id found", statusCode: 404);

        var allocation = AllocationMapper.ToAllocation(request);

        var finalAllocation = await AllocationRepository.AddAsync(allocation);

        return AllocationMapper.ToNodeAllocation(finalAllocation);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<NodeAllocationResponse>> UpdateAsync(
        [FromRoute] int nodeId,
        [FromRoute] int id,
        [FromBody] UpdateNodeAllocationRequest request
    )
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            return Problem("No allocation with that id found", statusCode: 404);

        AllocationMapper.Merge(request, allocation);
        await AllocationRepository.UpdateAsync(allocation);

        return AllocationMapper.ToNodeAllocation(allocation);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] int nodeId, [FromRoute] int id)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            return Problem("No allocation with that id found", statusCode: 404);

        await AllocationRepository.RemoveAsync(allocation);
        return NoContent();
    }

    [HttpPost("range")]
    public async Task<ActionResult> CreateRangeAsync(
        [FromRoute] int nodeId,
        [FromBody] CreateNodeAllocationRangeRequest request
    )
    {
        if (request.Start > request.End)
            return Problem("Invalid start and end specified", statusCode: 400);

        if (request.End - request.Start == 0)
            return Problem("Empty range specified", statusCode: 400);

        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (node == null)
            return Problem("No node with that id found", statusCode: 404);

        var existingAllocations = await AllocationRepository
            .Get()
            .Where(x => x.Port >= request.Start && x.Port <= request.End &&
                        x.IpAddress == request.IpAddress)
            .AsNoTracking()
            .ToArrayAsync();

        var ports = new List<int>();

        for (var i = request.Start; i < request.End; i++)
        {
            // Skip existing allocations
            if (existingAllocations.Any(x => x.Port == i))
                continue;

            ports.Add(i);
        }

        var allocations = ports
            .Select(port => new Allocation()
            {
                IpAddress = request.IpAddress,
                Port = port,
                Node = node
            })
            .ToArray();
        
        await AllocationRepository.RunTransactionAsync(async set => { await set.AddRangeAsync(allocations); });
        return NoContent();
    }

    [HttpDelete("all")]
    public async Task<ActionResult> DeleteAllAsync([FromRoute] int nodeId)
    {
        var allocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        await AllocationRepository.RunTransactionAsync(set => { set.RemoveRange(allocations); });
        return NoContent();
    }

    [HttpGet("free")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<CountedData<NodeAllocationResponse>>> GetFreeAsync(
        [FromRoute] int nodeId,
        [FromQuery] int startIndex,
        [FromQuery] int count,
        [FromQuery] int serverId = -1
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var node = NodeRepository
            .Get()
            .FirstOrDefault(x => x.Id == nodeId);

        if (node == null)
            return Problem("A node with this id could not be found", statusCode: 404);

        var freeAllocationsQuery = AllocationRepository
            .Get()
            .OrderBy(x => x.Id)
            .Where(x => x.Node.Id == node.Id)
            .Where(x => x.Server == null || x.Server.Id == serverId);

        var totalCount = await freeAllocationsQuery.CountAsync();

        var allocations = await freeAllocationsQuery
            .Skip(startIndex)
            .Take(count)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<NodeAllocationResponse>()
        {
            Items = allocations,
            TotalCount = totalCount
        };
    }
}