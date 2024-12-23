using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;

namespace MoonlightServers.Daemon.Http.Controllers.Statistics;

// This controller hosts endpoints for the statistics for the daemon application itself

[ApiController]
[Route("api/statistics/application")]
public class StatisticsApplicationController : Controller
{
    private readonly OwnProcessHelper ProcessHelper;

    public StatisticsApplicationController(OwnProcessHelper processHelper)
    {
        ProcessHelper = processHelper;
    }

    [HttpGet]
    public async Task<StatisticsApplicationResponse> Get()
    {
        return new StatisticsApplicationResponse()
        {
            Uptime = ProcessHelper.GetUptime(),
            MemoryUsage = ProcessHelper.GetMemoryUsage(),
            CpuUsage = ProcessHelper.CpuUsage()
        };
    }
}