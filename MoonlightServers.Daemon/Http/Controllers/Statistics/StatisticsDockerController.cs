using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;

namespace MoonlightServers.Daemon.Http.Controllers.Statistics;

// This controller hosts endpoints for the statistics for the docker environment

[Authorize]
[ApiController]
[Route("api/statistics/docker")]
public class StatisticsDockerController : Controller
{
    private readonly DockerInfoService DockerInfoService;

    public StatisticsDockerController(DockerInfoService dockerInfoService)
    {
        DockerInfoService = dockerInfoService;
    }

    [HttpGet]
    public async Task<StatisticsDockerResponse> GetAsync()
    {
        var usage = await DockerInfoService.GetDataUsageAsync();

        return new StatisticsDockerResponse
        {
            Version = await DockerInfoService.GetDockerVersionAsync(),
            ContainersReclaimable = usage.Containers.Reclaimable,
            ContainersUsed = usage.Containers.Used,
            BuildCacheReclaimable = usage.BuildCache.Reclaimable,
            BuildCacheUsed = usage.BuildCache.Used,
            ImagesUsed = usage.Images.Used,
            ImagesReclaimable = usage.Images.Reclaimable
        };
    }
}