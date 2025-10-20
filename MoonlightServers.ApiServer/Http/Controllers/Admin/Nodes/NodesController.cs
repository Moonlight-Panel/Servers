using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Common;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Requests.Admin.Nodes;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
public class NodesController : Controller
{
    private readonly DatabaseRepository<Node> NodeRepository;

    public NodesController(DatabaseRepository<Node> nodeRepository)
    {
        NodeRepository = nodeRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<CountedData<NodeResponse>>> GetAsync(
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var totalCount = await NodeRepository.Get().CountAsync();

        var items = await NodeRepository
            .Get()
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<NodeResponse>()
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<NodeResponse>> GetSingleAsync([FromRoute] int id)
    {
        var node = await NodeRepository
            .Get()
            .AsNoTracking()
            .ProjectToAdminResponse()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return Problem("No node with this id found", statusCode: 404);

        return node;
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<ActionResult<NodeResponse>> CreateAsync([FromBody] CreateNodeRequest request)
    {
        var node = NodeMapper.ToNode(request);

        node.TokenId = Formatter.GenerateString(6);
        node.Token = Formatter.GenerateString(32);

        var finalNode = await NodeRepository.AddAsync(node);

        return NodeMapper.ToAdminNodeResponse(finalNode);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.update")]
    public async Task<ActionResult<NodeResponse>> UpdateAsync([FromRoute] int id, [FromBody] UpdateNodeRequest request)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return Problem("No node with this id found", statusCode: 404);

        NodeMapper.Merge(request, node);
        await NodeRepository.UpdateAsync(node);

        return NodeMapper.ToAdminNodeResponse(node);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.delete")]
    public async Task<ActionResult> DeleteAsync([FromRoute] int id)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return Problem("No node with this id found", statusCode: 404);

        await NodeRepository.RemoveAsync(node);
        return Ok();
    }
}