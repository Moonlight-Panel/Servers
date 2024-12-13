using Microsoft.AspNetCore.Mvc;
using MoonCore.Attributes;
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
    
    private void ApplyNode(int id)
    {
        var node = NodeRepository
            .Get()
            .FirstOrDefault(x => x.Id == id);

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
        ApplyNode(nodeId);
        
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{nodeId:int}/allocations/{id:int}")]
    [RequirePermission("admin.servers.nodes.get")]
    public async Task<NodeAllocationDetailResponse> GetSingle([FromRoute] int nodeId, [FromRoute] int id)
    {
        ApplyNode(nodeId);
        
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost("{nodeId:int}/allocations")]
    [RequirePermission("admin.servers.nodes.create")]
    public async Task<NodeAllocationDetailResponse> Create([FromRoute] int nodeId, [FromBody] CreateNodeAllocationRequest request)
    {
        ApplyNode(nodeId);
        
        var allocation = Mapper.Map<Allocation>(request);
        allocation.Node = Node;

        var finalVariable = AllocationRepository.Add(allocation);

        return CrudHelper.MapToResult(finalVariable);
    }

    [HttpPatch("{nodeId:int}/allocations/{id:int}")]
    public async Task<NodeAllocationDetailResponse> Update([FromRoute] int nodeId, [FromRoute] int id, [FromBody] UpdateNodeAllocationRequest request)
    {
        ApplyNode(nodeId);
        
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{nodeId:int}/allocations/{id:int}")]
    public async Task Delete([FromRoute] int nodeId, [FromRoute] int id)
    {
        ApplyNode(nodeId);
        
        await CrudHelper.Delete(id);
    }
}