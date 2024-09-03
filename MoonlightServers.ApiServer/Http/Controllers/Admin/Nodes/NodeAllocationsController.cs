using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.App.Attributes;
using Moonlight.ApiServer.App.Exceptions;
using Moonlight.ApiServer.App.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Allocations;
using MoonlightServers.Shared.Http.Responses.Admin.Allocations;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("admin/servers/nodes/{rootItem:int}/allocations")]
public class NodeAllocationsController : BaseSubCrudController<Node, Allocation, DetailAllocationResponse, CreateAllocationRequest, DetailAllocationResponse, UpdateAllocationRequest, DetailAllocationResponse>
{
    public override Func<Node, List<Allocation>> Property => node => node.Allocations;
    
    public NodeAllocationsController(DatabaseRepository<Allocation> itemRepository, DatabaseRepository<Node> rootItemRepository, IHttpContextAccessor contextAccessor) : base(itemRepository, rootItemRepository, contextAccessor)
    {
        PermissionPrefix = "admin.servers.nodes.allocations";
    }

    [HttpPost]
    [RequirePermission("admin.servers.nodes.allocations.create")]
    public override async Task<ActionResult<DetailAllocationResponse>> Create(CreateAllocationRequest request)
    {
        if (ItemRepository.Get().Any(x => x.IpAddress == request.IpAddress && x.Port == request.Port))
            throw new ApiException("An allocation with this ip and port already exists", statusCode: 400);
        
        var item = Mapper.Map<Allocation>(request!);

        Property.Invoke(RootItem).Add(item);
        RootItemRepository.Update(RootItem);

        var response = Mapper.Map<DetailAllocationResponse>(item);

        return Ok(response);
    }

    [HttpPatch("{id}")]
    [RequirePermission("admin.servers.nodes.allocations.create")]
    public override async Task<ActionResult<DetailAllocationResponse>> Update(int id, UpdateAllocationRequest request)
    {
        var item = LoadItemById(id);
        
        if (ItemRepository.Get().Any(x => x.IpAddress == request.IpAddress && x.Port == request.Port && x.Id != item.Id))
            throw new ApiException("An allocation with this ip and port already exists", statusCode: 400);
        
        var mappedItem = Mapper.Map(item, request!, ignoreNullValues: true);

        ItemRepository.Update(mappedItem);

        var response = Mapper.Map<DetailAllocationResponse>(mappedItem);

        return Ok(response);
    }

    protected override IEnumerable<Node> IncludeRelations(IQueryable<Node> items)
        => items.Include(x => x.Allocations);
}