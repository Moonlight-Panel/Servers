using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoonlightServers.ApiServer.Http.Controllers.Remote.Nodes;

[ApiController]
[Route("api/remote/server/node")]
[Authorize(AuthenticationSchemes = "nodeAuthentication")]
public class NodeTripController : Controller
{
    [HttpGet("trip")]
    public Task GetAsync() => Task.CompletedTask;
}