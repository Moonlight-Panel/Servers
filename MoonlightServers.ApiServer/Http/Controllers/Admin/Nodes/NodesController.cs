using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeDetailResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<NodeDetailResponse> GetSingle([FromRoute] int id)
    {
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<NodeDetailResponse> Create([FromBody] CreateNodeRequest request)
    {
        var node = Mapper.Map<Node>(request);

        node.TokenId = Formatter.GenerateString(6);
        node.Token = Formatter.GenerateString(32);

        var finalNode = await NodeRepository.Add(node);

        return CrudHelper.MapToResult(finalNode);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.update")]
    public async Task<NodeDetailResponse> Update([FromRoute] int id, [FromBody] UpdateNodeRequest request)
    {
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.delete")]
    public async Task Delete([FromRoute] int id)
    {
        await CrudHelper.Delete(id);
    }
}