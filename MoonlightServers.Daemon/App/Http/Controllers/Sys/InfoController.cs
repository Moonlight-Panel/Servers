using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.App.Helpers;
using MoonlightServers.DaemonShared.Http.Resources.Sys;

namespace MoonlightServers.Daemon.App.Http.Controllers.Sys;

[ApiController]
[Route("system/info")]
public class InfoController : Controller
{
    private readonly HostHelper HostHelper;

    public InfoController(HostHelper hostHelper)
    {
        HostHelper = hostHelper;
    }

    [HttpGet]
    public async Task<ActionResult<SystemInfoResponse>> Get()
    {
        var memoryDetails = await HostHelper.GetMemoryDetails();
        var diskDetails = await HostHelper.GetDiskUsage();
        
        var response = new SystemInfoResponse()
        {
            CpuModel = await HostHelper.GetCpuModel(),
            CpuUsage = await HostHelper.GetCpuUsage(),
            
            Uptime = await HostHelper.GetUptime(),
            
            MemoryTotal = memoryDetails[0],
            MemoryFree = memoryDetails[1],
            MemoryAvailable = memoryDetails[2],
            MemoryCached = memoryDetails[3],
            
            SwapTotal = memoryDetails[4],
            SwapFree = memoryDetails[5],
            
            DiskTotal = diskDetails[0],
            DiskFree = diskDetails[1],
            DiskTotalInodes = diskDetails[2],
            DiskFreeInodes = diskDetails[3]
        };

        return Ok(response);
    }
}