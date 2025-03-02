using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;

namespace MoonlightServers.Daemon.Http.Controllers.Statistics;

// This controller hosts endpoints for the statistics for host system the daemon runs on

[Authorize]
[ApiController]
[Route("api/statistics/host")]
public class StatisticsHostController : Controller
{
    private readonly HostSystemHelper HostSystemHelper;

    public StatisticsHostController(HostSystemHelper hostSystemHelper)
    {
        HostSystemHelper = hostSystemHelper;
    }

    [HttpGet]
    public async Task<StatisticsHostResponse> Get()
    {
        return new()
        {
            OperatingSystem = HostSystemHelper.GetOsName()
        };
    }
}