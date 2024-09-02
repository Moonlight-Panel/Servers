using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
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

    protected override IEnumerable<Node> IncludeRelations(IQueryable<Node> items)
        => items.Include(x => x.Allocations);
}