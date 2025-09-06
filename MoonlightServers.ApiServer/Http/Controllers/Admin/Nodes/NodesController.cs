using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Models;
using MoonCore.Helpers;
using MoonCore.Models;
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
    public async Task<IPagedData<NodeResponse>> Get([FromQuery] PagedOptions options)
    {
        var count = await NodeRepository.Get().CountAsync();

        var items = await NodeRepository
            .Get()
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new PagedData<NodeResponse>()
        {
            Items = items,
            CurrentPage = options.Page,
            PageSize = options.PageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<ActionResult<NodeResponse>> GetSingle([FromRoute] int id)
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
    public async Task<ActionResult<NodeResponse>> Create([FromBody] CreateNodeRequest request)
    {
        var node = NodeMapper.ToNode(request);

        node.TokenId = Formatter.GenerateString(6);
        node.Token = Formatter.GenerateString(32);

        var finalNode = await NodeRepository.Add(node);

        return NodeMapper.ToAdminNodeResponse(finalNode);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.update")]
    public async Task<ActionResult<NodeResponse>> Update([FromRoute] int id, [FromBody] UpdateNodeRequest request)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return Problem("No node with this id found", statusCode: 404);

        NodeMapper.Merge(request, node);
        await NodeRepository.Update(node);

        return NodeMapper.ToAdminNodeResponse(node);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.delete")]
    public async Task<ActionResult> Delete([FromRoute] int id)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            return Problem("No node with this id found", statusCode: 404);
        
        await NodeRepository.Remove(node);
        return Ok();
    }
}