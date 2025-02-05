using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.PermFilter;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
public class NodeAllocationsController : Controller
{
    private readonly CrudHelper<Allocation, NodeAllocationDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly DatabaseRepository<Allocation> AllocationRepository;

    private Node Node;

    public NodeAllocationsController(CrudHelper<Allocation, NodeAllocationDetailResponse> crudHelper, DatabaseRepository<Node> nodeRepository, DatabaseRepository<Allocation> allocationRepository)
    {
        CrudHelper = crudHelper;
        NodeRepository = nodeRepository;
        AllocationRepository = allocationRepository;
    }
    
    private async Task ApplyNode(int id)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            throw new HttpApiException("A node with this id could not be found", 404);

        Node = node;

        CrudHelper.QueryModifier = variables =>
            variables.Where(x => x.Node.Id == node.Id);
    }

    [HttpGet("{nodeId:int}/allocations")]
    [RequirePermission("admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationDetailResponse>> Get([FromRoute] int nodeId, [FromQuery] int page, [FromQuery] int pageSize)
    {
        await ApplyNode(nodeId);
        
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{nodeId:int}/allocations/{id:int}")]
    [RequirePermission("admin.servers.nodes.get")]
    public async Task<NodeAllocationDetailResponse> GetSingle([FromRoute] int nodeId, [FromRoute] int id)
    {
        await ApplyNode(nodeId);
        
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost("{nodeId:int}/allocations")]
    [RequirePermission("admin.servers.nodes.create")]
    public async Task<NodeAllocationDetailResponse> Create([FromRoute] int nodeId, [FromBody] CreateNodeAllocationRequest request)
    {
        await ApplyNode(nodeId);
        
        var allocation = Mapper.Map<Allocation>(request);
        allocation.Node = Node;

        var finalVariable = await AllocationRepository.Add(allocation);

        return CrudHelper.MapToResult(finalVariable);
    }

    [HttpPatch("{nodeId:int}/allocations/{id:int}")]
    public async Task<NodeAllocationDetailResponse> Update([FromRoute] int nodeId, [FromRoute] int id, [FromBody] UpdateNodeAllocationRequest request)
    {
        await ApplyNode(nodeId);
        
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{nodeId:int}/allocations/{id:int}")]
    public async Task Delete([FromRoute] int nodeId, [FromRoute] int id)
    {
        await ApplyNode(nodeId);
        
        await CrudHelper.Delete(id);
    }

    [HttpPost("{nodeId:int}/allocations/range")]
    public async Task CreateRange([FromRoute] int nodeId, [FromBody] CreateNodeAllocationRangeRequest rangeRequest)
    {
        await ApplyNode(nodeId);
        
        var existingAllocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        var ports = new List<int>();

        for (var i = rangeRequest.Start; i < rangeRequest.End; i++)
        {
            // Skip existing allocations
            if(existingAllocations.Any(x => x.Port == i && x.IpAddress == rangeRequest.IpAddress))
                continue;
            
            ports.Add(i);
        }

        var allocations = ports
            .Select(port => new Allocation()
            {
                IpAddress = rangeRequest.IpAddress,
                Port = port,
                Node = Node
            })
            .ToArray();
        
        // TODO: Add AddRange in database repository
        foreach (var allocation in allocations)
            await AllocationRepository.Add(allocation);
    }

    [HttpDelete("{nodeId:int}/allocations/all")]
    public async Task DeleteAll([FromRoute] int nodeId)
    {
        await ApplyNode(nodeId);
        
        var allocations = AllocationRepository
            .Get()
            .Where(x => x.Node.Id == nodeId)
            .ToArray();

        foreach (var allocation in allocations)
            await AllocationRepository.Remove(allocation);
    }
    
    [HttpGet("{nodeId:int}/allocations/free")]
    [RequirePermission("admin.servers.nodes.get")]
    public async Task<IPagedData<NodeAllocationDetailResponse>> GetFree([FromRoute] int nodeId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] int serverId = -1)
    {
        var node = NodeRepository
            .Get()
            .FirstOrDefault(x => x.Id == nodeId);

        if (node == null)
            throw new HttpApiException("A node with this id could not be found", 404);

        Node = node;

        CrudHelper.QueryModifier = variables => variables
                .Where(x => x.Node.Id == node.Id)
                .Where(x => x.Server == null || x.Server.Id == serverId);
        
        return await CrudHelper.Get(page, pageSize);
    }
}