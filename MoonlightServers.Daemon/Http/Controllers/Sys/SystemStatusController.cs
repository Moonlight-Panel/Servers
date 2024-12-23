using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Sys;

namespace MoonlightServers.Daemon.Http.Controllers.Sys;

[ApiController]
[Route("api/system/status")]
public class SystemStatusController : Controller
{
    private readonly RemoteService RemoteService;

    public SystemStatusController(RemoteService remoteService)
    {
        RemoteService = remoteService;
    }

    public async Task<SystemStatusResponse> Get()
    {
        SystemStatusResponse response;
        
        var sw = new Stopwatch();
        sw.Start();
        
        try
        {
            await RemoteService.GetStatus();
            
            sw.Stop();

            response = new()
            {
                TripSuccess = true,
                TripTime = sw.Elapsed,
                Version = "2.1.0" // TODO: Set global
            };
        }
        catch (Exception e)
        {
            sw.Stop();

            response = new()
            {
                TripError = e.Message,
                TripTime = sw.Elapsed,
                TripSuccess = false,
                Version = "2.1.0" // TODO: Set global
            };
        }

        return response;
    }
}