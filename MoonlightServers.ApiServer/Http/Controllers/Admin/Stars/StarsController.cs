using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Models;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Requests.Admin.Stars;
using MoonlightServers.Shared.Http.Responses.Admin.Stars;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars")]
public class StarsController : Controller
{
    private readonly DatabaseRepository<Star> StarRepository;

    public StarsController(DatabaseRepository<Star> starRepository)
    {
        StarRepository = starRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<ActionResult<IPagedData<StarResponse>>> Get([FromQuery] PagedOptions options)
    {
        var count = await StarRepository.Get().CountAsync();

        var stars = await StarRepository
            .Get()
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new PagedData<StarResponse>()
        {
            CurrentPage = options.Page,
            Items = stars,
            PageSize = options.PageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<ActionResult<StarResponse>> GetSingle([FromRoute] int id)
    {
        var star = await StarRepository
            .Get()
            .AsNoTracking()
            .ProjectToAdminResponse()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            return Problem("No star with that id found", statusCode: 404);

        return star;
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<ActionResult<StarResponse>> Create([FromBody] CreateStarRequest request)
    {
        var star = StarMapper.ToStar(request);

        // Default values
        star.DonateUrl = null;
        star.UpdateUrl = null;
        star.Version = "1.0.0";
        star.StartupCommand = "echo Starting up :)";
        star.StopCommand = "^C";
        star.OnlineDetection = "Online text";
        star.InstallShell = "/bin/bash";
        star.InstallDockerImage = "debian:latest";
        star.InstallScript = "echo Installing...";
        star.RequiredAllocations = 1;
        star.AllowDockerImageChange = false;
        star.DefaultDockerImage = -1;
        star.ParseConfiguration = "[]";

        var finalStar = await StarRepository.Add(star);

        return StarMapper.ToAdminResponse(finalStar);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.update")]
    public async Task<ActionResult<StarResponse>> Update(
        [FromRoute] int id,
        [FromBody] UpdateStarRequest request
    )
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            return Problem("No star with that id found", statusCode: 404);
        
        StarMapper.Merge(request, star);
        await StarRepository.Update(star);
        
        return StarMapper.ToAdminResponse(star);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.delete")]
    public async Task<ActionResult> Delete([FromRoute] int id)
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            return Problem("No star with that id found", statusCode: 404);
        
        await StarRepository.Remove(star);
        return NoContent();
    }
}