using Microsoft.AspNetCore.Mvc;

namespace MoonlightServers.ApiServer.Http.Controllers.Remote.Node;

[ApiController]
[Route("api/servers/remote/node")]
public class NodeTripController : Controller
{
    [HttpGet("trip")]
    public Task Get() => Task.CompletedTask;
}