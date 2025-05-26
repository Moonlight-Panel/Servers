using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;

namespace MoonlightServers.Daemon.Http.Controllers.Statistics;

[Authorize]
[ApiController]
[Route("api/statistics")]
public class StatisticsController : Controller
{
    private readonly HostSystemHelper HostSystemHelper;

    public StatisticsController(HostSystemHelper hostSystemHelper)
    {
        HostSystemHelper = hostSystemHelper;
    }

    [HttpGet]
    public async Task<StatisticsResponse> Get()
    {
        var response = new StatisticsResponse();

        var cpuUsage = await HostSystemHelper.GetCpuUsage();

        response.Cpu.Model = cpuUsage.Model;
        response.Cpu.Usage = cpuUsage.OverallUsage;
        response.Cpu.UsagePerCore = cpuUsage.PerCoreUsage;

        var memoryUsage = await HostSystemHelper.GetMemoryUsage();

        response.Memory.Available = memoryUsage.Available;
        response.Memory.Cached = memoryUsage.Cached;
        response.Memory.Free = memoryUsage.Free;
        response.Memory.Total = memoryUsage.Total;
        response.Memory.SwapTotal = memoryUsage.SwapTotal;
        response.Memory.SwapFree = memoryUsage.SwapFree;

        var diskDetails = await HostSystemHelper.GetDiskUsages();

        response.Disks = diskDetails.Select(x => new StatisticsResponse.DiskData()
        {
            Device = x.Device,
            MountPath = x.MountPath,
            DiskFree = x.DiskFree,
            DiskTotal = x.DiskTotal,
            InodesFree = x.InodesFree,
            InodesTotal = x.InodesTotal
        }).ToArray();
        
        return response;
    }
}