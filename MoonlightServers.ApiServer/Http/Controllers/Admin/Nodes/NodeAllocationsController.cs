using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
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

    [HttpGet("")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationResponse>> Get(
        [FromRoute] int nodeId,
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var count = await AllocationRepository.Get().CountAsync(x => x.Node.Id == nodeId);

        var allocations = await AllocationRepository
            .Get()
            .Skip(page * pageSize)
            .Take(pageSize)
            .Where(x => x.Node.Id == nodeId)
            .ToArrayAsync();

        var mappedAllocations = allocations
            .Select(AllocationMapper.ToNodeAllocation)
            .ToArray();

        return new PagedData<NodeAllocationResponse>()
        {
            Items = mappedAllocations,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : (count - 1) / pageSize
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<NodeAllocationResponse> GetSingle([FromRoute] int nodeId, [FromRoute] int id)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            throw new HttpApiException("No allocation with that id found", 404);

        return AllocationMapper.ToNodeAllocation(allocation);
    }

    [HttpPost("")]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<NodeAllocationResponse> Create(
        [FromRoute] int nodeId,
        [FromBody] CreateNodeAllocationRequest request
    )
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (node == null)
            throw new HttpApiException("No node with that id found", 404);

        var allocation = AllocationMapper.ToAllocation(request);

        var finalAllocation = await AllocationRepository.Add(allocation);

        return AllocationMapper.ToNodeAllocation(finalAllocation);
    }

    [HttpPatch("{id:int}")]
    public async Task<NodeAllocationResponse> Update(
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
            throw new HttpApiException("No allocation with that id found", 404);

        AllocationMapper.Merge(request, allocation);
        await AllocationRepository.Update(allocation);

        return AllocationMapper.ToNodeAllocation(allocation);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete([FromRoute] int nodeId, [FromRoute] int id)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            throw new HttpApiException("No allocation with that id found", 404);

        await AllocationRepository.Remove(allocation);
    }

    [HttpPost("range")]
    public async Task CreateRange([FromRoute] int nodeId, [FromBody] CreateNodeAllocationRangeRequest rangeRequest)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (node == null)
            throw new HttpApiException("No node with that id found", 404);

        var existingAllocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        var ports = new List<int>();

        for (var i = rangeRequest.Start; i < rangeRequest.End; i++)
        {
            // Skip existing allocations
            if (existingAllocations.Any(x => x.Port == i && x.IpAddress == rangeRequest.IpAddress))
                continue;

            ports.Add(i);
        }

        var allocations = ports
            .Select(port => new Allocation()
            {
                IpAddress = rangeRequest.IpAddress,
                Port = port,
                Node = node
            })
            .ToArray();

        await AllocationRepository.RunTransaction(async set => { await set.AddRangeAsync(allocations); });
    }

    [HttpDelete("all")]
    public async Task DeleteAll([FromRoute] int nodeId)
    {
        var allocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        await AllocationRepository.RunTransaction(set => { set.RemoveRange(allocations); });
    }

    [HttpGet("free")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationResponse>> GetFree(
        [FromRoute] int nodeId,
        [FromQuery] int page,
        [FromQuery] [Range(1, 100)] int pageSize,
        [FromQuery] int serverId = -1
    )
    {
        var node = NodeRepository
            .Get()
            .FirstOrDefault(x => x.Id == nodeId);

        if (node == null)
            throw new HttpApiException("A node with this id could not be found", 404);

        var freeAllocationsQuery = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == node.Id)
            .Where(x => x.Server == null || x.Server.Id == serverId);

        var count = await freeAllocationsQuery.CountAsync();
        var allocations = await freeAllocationsQuery.ToArrayAsync();

        var mappedAllocations = allocations
            .Select(AllocationMapper.ToNodeAllocation)
            .ToArray();

        return new PagedData<NodeAllocationResponse>()
        {
            Items = mappedAllocations,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : (count - 1) / pageSize
        };
    }
}