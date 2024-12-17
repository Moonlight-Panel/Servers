using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Nodes;
using MoonlightServers.Shared.Http.Responses.Admin.NodeAllocations;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
public class NodesController : Controller
{
    private readonly CrudHelper<Node, NodeDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Node> NodeRepository;

    public NodesController(
        CrudHelper<Node, NodeDetailResponse> crudHelper,
        DatabaseRepository<Node> nodeRepository
    )
    {
        CrudHelper = crudHelper;
        NodeRepository = nodeRepository;
    }

    [HttpGet]
    public async Task<IPagedData<NodeDetailResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<NodeDetailResponse> GetSingle([FromRoute] int id)
    {
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost]
    public async Task<NodeDetailResponse> Create([FromBody] CreateNodeRequest request)
    {
        var node = Mapper.Map<Node>(request);

        node.Token = Formatter.GenerateString(32);

        var finalNode = NodeRepository.Add(node);

        return CrudHelper.MapToResult(finalNode);
    }

    [HttpPatch("{id:int}")]
    public async Task<NodeDetailResponse> Update([FromRoute] int id, [FromBody] UpdateNodeRequest request)
    {
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete([FromRoute] int id)
    {
        await CrudHelper.Delete(id);
    }
}