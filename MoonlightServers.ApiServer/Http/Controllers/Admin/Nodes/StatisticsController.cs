using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses.Admin.Nodes.Statistics;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Nodes;

[ApiController]
[Route("api/admin/servers/nodes")]
[Authorize(Policy = "permissions:admin.servers.nodes.statistics")]
public class StatisticsController : Controller
{
    private readonly NodeService NodeService;
    private readonly DatabaseRepository<Node> NodeRepository;

    public StatisticsController(NodeService nodeService, DatabaseRepository<Node> nodeRepository)
    {
        NodeService = nodeService;
        NodeRepository = nodeRepository;
    }

    [HttpGet("{nodeId:int}/statistics")]
    public async Task<StatisticsResponse> Get([FromRoute] int nodeId)
    {
        var node = await GetNode(nodeId);
        var statistics = await NodeService.GetStatistics(node);

        return new()
        {
            Cpu = new()
            {
                Model = statistics.Cpu.Model,
                Usage = statistics.Cpu.Usage,
                UsagePerCore = statistics.Cpu.UsagePerCore
            },
            Memory = new()
            {
                Available = statistics.Memory.Available,
                Total = statistics.Memory.Total,
                Cached = statistics.Memory.Cached,
                Free = statistics.Memory.Free,
                SwapFree = statistics.Memory.SwapFree,
                SwapTotal = statistics.Memory.SwapTotal
            },
            Disks = statistics.Disks.Select(x => new StatisticsResponse.DiskData()
            {
                Device = x.Device,
                DiskFree = x.DiskFree,
                DiskTotal = x.DiskTotal,
                InodesFree = x.InodesFree,
                InodesTotal = x.InodesTotal,
                MountPath = x.MountPath
            }).ToArray()
        };
    }

    [HttpGet("{nodeId:int}/statistics/docker")]
    public async Task<DockerStatisticsResponse> GetDocker([FromRoute] int nodeId)
    {
        var node = await GetNode(nodeId);
        var statistics = await NodeService.GetDockerStatistics(node);

        return new()
        {
            BuildCacheReclaimable = statistics.BuildCacheReclaimable,
            BuildCacheUsed = statistics.BuildCacheUsed,
            ContainersReclaimable = statistics.ContainersReclaimable,
            ContainersUsed = statistics.ContainersUsed,
            ImagesReclaimable = statistics.ImagesReclaimable,
            ImagesUsed = statistics.ImagesUsed,
            Version = statistics.Version
        };
    }
    
    private async Task<Node> GetNode(int nodeId)
    {
        var result = await NodeRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == nodeId);

        if (result == null)
            throw new HttpApiException("A node with this id could not be found", 404);

        return result;
    }
}