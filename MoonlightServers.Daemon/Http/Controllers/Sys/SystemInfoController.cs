using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.Http.Responses.Sys;

namespace MoonlightServers.Daemon.Http.Controllers.Sys;

[ApiController]
[Route("api/system/info")]
public class SystemInfoController : Controller
{
    private readonly SystemInfoService SystemInfoService;
    private readonly DockerInfoService DockerInfoService;

    public SystemInfoController(SystemInfoService systemInfoService, DockerInfoService dockerInfoService)
    {
        SystemInfoService = systemInfoService;
        DockerInfoService = dockerInfoService;
    }

    [HttpGet]
    public async Task<SystemInfoResponse> GetInfo()
    {
        return new SystemInfoResponse()
        {
            Uptime = await SystemInfoService.GetUptime(),
            Version = "2.1.0",
            CpuUsage = await SystemInfoService.GetCpuUsage(),
            DockerVersion = await DockerInfoService.GetDockerVersion(),
            MemoryUsage = await SystemInfoService.GetMemoryUsage(),
            OsName = await SystemInfoService.GetOsName()
        };
    }
}