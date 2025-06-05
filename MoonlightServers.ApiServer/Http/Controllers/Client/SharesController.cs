using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class SharesController : Controller
{
    
}