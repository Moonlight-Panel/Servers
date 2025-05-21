using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
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

    [HttpGet("{nodeId:int}/allocations")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationDetailResponse>> Get(
        [FromRoute] int nodeId,
        [FromQuery] int page,
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

        var mappedAllocations = allocations.Select(x => new NodeAllocationDetailResponse()
        {
            Id = x.Id,
            IpAddress = x.IpAddress,
            Port = x.Port
        }).ToArray();

        return new PagedData<NodeAllocationDetailResponse>()
        {
            Items = mappedAllocations,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : (count - 1) / pageSize
        };
    }

    [HttpGet("{nodeId:int}/allocations/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<NodeAllocationDetailResponse> GetSingle([FromRoute] int nodeId, [FromRoute] int id)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            throw new HttpApiException("No allocation with that id found", 404);

        return new()
        {
            Id = allocation.Id,
            IpAddress = allocation.IpAddress,
            Port = allocation.Port
        };
    }

    [HttpPost("{nodeId:int}/allocations")]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<NodeAllocationDetailResponse> Create(
        [FromRoute] int nodeId,
        [FromBody] CreateNodeAllocationRequest request
    )
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (node == null)
            throw new HttpApiException("No node with that id found", 404);

        var allocation = new Allocation
        {
            IpAddress = request.IpAddress,
            Port = request.Port,
            Node = node
        };

        var finalVariable = await AllocationRepository.Add(allocation);

        return new()
        {
            Id = finalVariable.Id,
            IpAddress = finalVariable.IpAddress,
            Port = finalVariable.Port
        };
    }

    [HttpPatch("{nodeId:int}/allocations/{id:int}")]
    public async Task<NodeAllocationDetailResponse> Update([FromRoute] int nodeId, [FromRoute] int id,
        [FromBody] UpdateNodeAllocationRequest request)
    {
        var allocation = await AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (allocation == null)
            throw new HttpApiException("No allocation with that id found", 404);

        allocation.IpAddress = request.IpAddress;
        allocation.Port = request.Port;

        await AllocationRepository.Update(allocation);
        
        return new()
        {
            Id = allocation.Id,
            IpAddress = allocation.IpAddress,
            Port = allocation.Port
        };
    }

    [HttpDelete("{nodeId:int}/allocations/{id:int}")]
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

    [HttpPost("{nodeId:int}/allocations/range")]
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

    [HttpDelete("{nodeId:int}/allocations/all")]
    public async Task DeleteAll([FromRoute] int nodeId)
    {
        var allocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        await AllocationRepository.RunTransaction(set => { set.RemoveRange(allocations); });
    }

    [HttpGet("{nodeId:int}/allocations/free")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationDetailResponse>> GetFree([FromRoute] int nodeId, [FromQuery] int page,
        [FromQuery][Range(1, 100)] int pageSize, [FromQuery] int serverId = -1)
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

        var mappedAllocations = allocations.Select(x => new NodeAllocationDetailResponse()
        {
            Id = x.Id,
            IpAddress = x.IpAddress,
            Port = x.Port
        }).ToArray();

        return new PagedData<NodeAllocationDetailResponse>()
        {
            Items = mappedAllocations,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : (count - 1) / pageSize
        };
    }
}