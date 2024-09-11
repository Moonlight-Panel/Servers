using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.App.Attributes;
using Moonlight.ApiServer.App.Exceptions;
using Moonlight.ApiServer.App.Helpers;
using MoonlightServers.DaemonShared.Http.Resources.Sys;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Extensions;
using MoonlightServers.Shared.Http.Requests.Admin.Nodes;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("admin/servers/nodes")]
public class NodesController : BaseCrudController<Node, DetailNodeResponse, CreateNodeRequest, DetailNodeResponse, UpdateNodeRequest, DetailNodeResponse>
{ 
    public NodesController(DatabaseRepository<Node> itemRepository) : base(itemRepository)
    {
        PermissionPrefix = "admin.servers.nodes";
    }

    [HttpPost]
    [RequirePermission("admin.servers.nodes.create")]
    public override async Task<ActionResult<DetailNodeResponse>> Create(CreateNodeRequest request)
    {
        ValidateFqdn(request.Fqdn, request.SslEnabled);

        var node = Mapper.Map<Node>(request);
        node.Token = Formatter.GenerateString(32);

        var finalNode = ItemRepository.Add(node);

        return Ok(Mapper.Map<DetailNodeResponse>(finalNode));
    }

    [HttpPatch("{id}")]
    [RequirePermission("admin.servers.nodes.update")]
    public override async Task<ActionResult<DetailNodeResponse>> Update(int id, UpdateNodeRequest request)
    {
        ValidateFqdn(request.Fqdn, request.SslEnabled);
        
        var item = LoadItemById(id);

        item = Mapper.Map(item, request);
        
        ItemRepository.Update(item);

        return Ok(Mapper.Map<DetailNodeResponse>(item));
    }

    [HttpGet("{id}/status")]
    [RequirePermission("admin.servers.nodes.status")]
    public async Task<ActionResult<StatusNodeResponse>> Status(int id)
    {
        var node = LoadItemById(id);

        using var httpClient = node.CreateClient();

        SystemInfoResponse response;
        
        try
        {
            response = await httpClient.GetJson<SystemInfoResponse>("system/info");
        }
        catch (HttpRequestException e)
        {
            throw new ApiException(
                "The requested node's api server was not reachable",
                e.Message,
                statusCode: 502
            );
        }
        
        var result = Mapper.Map<StatusNodeResponse>(response);

        return Ok(result);
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
}