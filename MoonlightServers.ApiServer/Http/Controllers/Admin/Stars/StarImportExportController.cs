using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Responses.Admin.Stars;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars")]
public class StarImportExportController : Controller
{
    private readonly StarImportExportService ImportExportService;

    public StarImportExportController(StarImportExportService importExportService)
    {
        ImportExportService = importExportService;
    }

    [HttpGet("{starId:int}/export")]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task Export([FromRoute] int starId)
    {
        var exportedStar = await ImportExportService.Export(starId);

        Response.StatusCode = 200;
        Response.ContentType = "application/json";
        await Response.WriteAsync(exportedStar);
    }

    [HttpPost("import")]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<StarDetailResponse> Import()
    {
        if (Request.Form.Files.Count == 0)
            throw new HttpApiException("No file to import provided", 400);
        
        if (Request.Form.Files.Count > 1)
            throw new HttpApiException("Only one file to import allowed", 400);

        var file = Request.Form.Files[0];
        await using var stream = file.OpenReadStream();
        using var sr = new StreamReader(stream, Encoding.UTF8);
        var content = await sr.ReadToEndAsync();

        var star = await ImportExportService.Import(content);

        return Mapper.Map<StarDetailResponse>(star);
    }
}