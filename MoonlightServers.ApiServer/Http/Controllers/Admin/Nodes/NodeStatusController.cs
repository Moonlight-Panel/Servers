using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes.Sys;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
public class NodeStatusController : Controller
{
    private readonly DatabaseRepository<Node> NodeRepository;
    private readonly NodeService NodeService;

    public NodeStatusController(DatabaseRepository<Node> nodeRepository, NodeService nodeService)
    {
        NodeRepository = nodeRepository;
        NodeService = nodeService;
    }

    [HttpGet("{nodeId:int}/system/status")]
    [Authorize(Policy = "permissions:admin.servers.nodes.status")]
    public async Task<ActionResult<NodeSystemStatusResponse>> GetStatus([FromRoute] int nodeId)
    {
        var node = await GetNode(nodeId);

        if (node.Value == null)
            return node.Result ?? Problem("Unable to retrieve node");
        
        NodeSystemStatusResponse response;

        var sw = new Stopwatch();
        sw.Start();
        
        try
        {
            var statusResponse = await NodeService.GetSystemStatus(node.Value);
            
            sw.Stop();

            response = new()
            {
                Version = statusResponse.Version,
                RoundtripError = statusResponse.TripError,
                RoundtripSuccess = statusResponse.TripSuccess,
                RoundtripTime = statusResponse.TripTime + sw.Elapsed,
                RoundtripRemoteFailure = !statusResponse.TripSuccess // When the remote trip failed, it's the remotes fault
            };
        }
        catch (Exception e)
        {
            sw.Stop();

            response = new()
            {
                Version = "Unknown",
                RoundtripError = e.Message,
                RoundtripSuccess = false,
                RoundtripTime = sw.Elapsed,
                RoundtripRemoteFailure = false
            };
        }

        return response;
    }

    private async Task<ActionResult<Node>> GetNode(int nodeId)
    {
        var result = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (result == null)
            return Problem("A node with this id could not be found", statusCode: 404);

        return result;
    }
}