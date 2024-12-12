using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.Http.Responses.Sys;

namespace MoonlightServers.Daemon.Http.Controllers.Sys;

[ApiController]
[Route("api/system/dataUsage")]
public class SystemDataUsageController : Controller
{
    private readonly DockerInfoService DockerInfoService;

    public SystemDataUsageController(DockerInfoService dockerInfoService)
    {
        DockerInfoService = dockerInfoService;
    }

    [HttpGet]
    public async Task<SystemDataUsageResponse> Get()
    {
        var report = await DockerInfoService.GetDataUsage();

        return new SystemDataUsageResponse()
        {
            ImagesReclaimable = report.Images.Reclaimable,
            ImagesUsed = report.Images.Used,
            ContainersReclaimable = report.Containers.Reclaimable,
            ContainersUsed = report.Containers.Used,
            BuildCacheReclaimable = report.BuildCache.Reclaimable,
            BuildCacheUsed = report.BuildCache.Used
        };
    }
}