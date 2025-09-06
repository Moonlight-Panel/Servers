using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Extended.Models;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Requests.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars/{starId:int}/dockerImages")]
public class StarDockerImagesController : Controller
{
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<StarDockerImage> DockerImageRepository;

    public StarDockerImagesController(
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<StarDockerImage> dockerImageRepository
    )
    {
        StarRepository = starRepository;
        DockerImageRepository = dockerImageRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<ActionResult<IPagedData<StarDockerImageResponse>>> Get(
        [FromRoute] int starId,
        [FromQuery] PagedOptions options
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            return Problem("No star with this id found", statusCode: 404);

        var query = DockerImageRepository
            .Get()
            .Where(x => x.Star.Id == starId);

        var count = await query.CountAsync();

        var dockerImages = await query
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new PagedData<StarDockerImageResponse>()
        {
            Items = dockerImages,
            CurrentPage = options.Page,
            PageSize = options.PageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<ActionResult<StarDockerImageResponse>> GetSingle([FromRoute] int starId, [FromRoute] int id)
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            return Problem("No star with this id found", statusCode: 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .Where(x => x.Id == id && x.Star.Id == starId)
            .ProjectToAdminResponse()
            .FirstOrDefaultAsync();

        if (dockerImage == null)
            return Problem("No star docker image with this id found", statusCode: 404);

        return dockerImage;
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<ActionResult<StarDockerImageResponse>> Create(
        [FromRoute] int starId,
        [FromBody] CreateStarDockerImageRequest request
    )
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == starId);

        if (star == null)
            return Problem("No star with this id found", statusCode: 404);

        var dockerImage = DockerImageMapper.ToDockerImage(request);
        dockerImage.Star = star;

        var finalDockerImage = await DockerImageRepository.Add(dockerImage);

        return DockerImageMapper.ToAdminResponse(finalDockerImage);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<ActionResult<StarDockerImageResponse>> Update(
        [FromRoute] int starId,
        [FromRoute] int id,
        [FromBody] UpdateStarDockerImageRequest request
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            return Problem("No star with this id found", statusCode: 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (dockerImage == null)
            return Problem("No star docker image with this id found", statusCode: 404);

        DockerImageMapper.Merge(request, dockerImage);
        await DockerImageRepository.Update(dockerImage);

        return DockerImageMapper.ToAdminResponse(dockerImage);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<ActionResult> Delete([FromRoute] int starId, [FromRoute] int id)
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            return Problem("No star with this id found", statusCode: 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (dockerImage == null)
            return Problem("No star docker image with this id found", statusCode: 404);
        
        await DockerImageRepository.Remove(dockerImage);
        return NoContent();
    }
}