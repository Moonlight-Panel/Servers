using Microsoft.AspNetCore.Mvc;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses;

namespace MoonlightServers.ApiServer.Http.Controllers;

[ApiController]
[Route("api/example")]
public class ExampleController : Controller
{
    private readonly ExampleService ExampleService;

    public ExampleController(ExampleService exampleService)
    {
        ExampleService = exampleService;
    }

    [HttpGet]
    public async Task<ExampleResponse> Get()
    {
        var val = await ExampleService.GetValue();

        return new ExampleResponse()
        {
            Number = val
        };
    }
}