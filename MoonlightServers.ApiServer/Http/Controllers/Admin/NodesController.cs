using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.App.Attributes;
using Moonlight.ApiServer.App.Exceptions;
using Moonlight.ApiServer.App.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Nodes;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin;

[ApiController]
[Route("admin/servers/nodes")]
public class NodesController : BaseCrudController<Node, DetailNodeResponse, CreateNodeRequest, DetailNodeResponse, UpdateNodeRequest, DetailNodeResponse>
{
    private DatabaseRepository<Node> NodeRepository;
    private DatabaseRepository<Allocation> AllocationRepository;
    
    public NodesController(DatabaseRepository<Node> itemRepository, DatabaseRepository<Allocation> allocationRepository) : base(itemRepository)
    {
        PermissionPrefix = "admin.servers.nodes";
        
        NodeRepository = itemRepository;
        AllocationRepository = allocationRepository;
    }

    [HttpPost]
    [RequirePermission("admin.servers.nodes.create")]
    public override async Task<ActionResult<DetailNodeResponse>> Create(CreateNodeRequest request)
    {
        ValidateFqdn(request.Fqdn, request.SslEnabled);

        var node = Mapper.Map<Node>(request);
        node.Token = Formatter.GenerateString(32);

        var finalNode = NodeRepository.Add(node);

        return Ok(Mapper.Map<DetailNodeResponse>(finalNode));
    }

    [HttpPatch("{id}")]
    [RequirePermission("admin.servers.nodes.update")]
    public override async Task<ActionResult<DetailNodeResponse>> Update(int id, UpdateNodeRequest request)
    {
        ValidateFqdn(request.Fqdn, request.SslEnabled);
        
        var item = LoadItemById(id);

        item = Mapper.Map(item, request);
        
        NodeRepository.Update(item);

        return Ok(Mapper.Map<DetailNodeResponse>(item));
    }

    private void ValidateFqdn(string fqdn, bool ssl)
    {
        if (ssl)
        {
            // Is it a valid domain?
            if (Regex.IsMatch(fqdn, "^(?!-)(?:[a-zA-Z\\d-]{0,62}[a-zA-Z\\d]\\.)+(?:[a-zA-Z]{2,})$"))
                return;

            throw new ApiException("The fqdn needs to be a valid domain. If you want to use an ip address as the fqdn, disable ssl for this node", statusCode: 400);
        }
        else
        {
            // Is it a valid domain?
            if (Regex.IsMatch(fqdn, "^(?!-)(?:[a-zA-Z\\d-]{0,62}[a-zA-Z\\d]\\.)+(?:[a-zA-Z]{2,})$"))
                return;

            // Is it a valid ip?
            if (Regex.IsMatch(fqdn, "^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                return;

            throw new ApiException("The fqdn needs to be either a domain or an ip", statusCode: 400);
        }
    }
    
    // Allocations
    
}