using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
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

    public NodesController(
        DatabaseRepository<Node> nodeRepository
    )
    {
        NodeRepository = nodeRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<IPagedData<NodeResponse>> Get(
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var query = NodeRepository
            .Get();

        var count = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var mappedItems = items
            .Select(NodeMapper.ToAdminNodeResponse)
            .ToArray();

        return new PagedData<NodeResponse>()
        {
            Items = mappedItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.get")]
    public async Task<NodeResponse> GetSingle([FromRoute] int id)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            throw new HttpApiException("No node with this id found", 404);

        return NodeMapper.ToAdminNodeResponse(node);
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.nodes.create")]
    public async Task<NodeResponse> Create([FromBody] CreateNodeRequest request)
    {
        var node = NodeMapper.ToNode(request);

        node.TokenId = Formatter.GenerateString(6);
        node.Token = Formatter.GenerateString(32);

        var finalNode = await NodeRepository.Add(node);

        return NodeMapper.ToAdminNodeResponse(finalNode);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.update")]
    public async Task<NodeResponse> Update([FromRoute] int id, [FromBody] UpdateNodeRequest request)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            throw new HttpApiException("No node with this id found", 404);

        NodeMapper.Merge(request, node);
        await NodeRepository.Update(node);

        return NodeMapper.ToAdminNodeResponse(node);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.nodes.delete")]
    public async Task Delete([FromRoute] int id)
    {
        var node = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (node == null)
            throw new HttpApiException("No node with this id found", 404);

        await NodeRepository.Remove(node);
    }
}