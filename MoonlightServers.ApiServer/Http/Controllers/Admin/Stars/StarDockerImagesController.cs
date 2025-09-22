using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.Abstractions;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<CountedData<StarDockerImageResponse>>> GetAsync(
        [FromRoute] int starId,
        [FromQuery] int startIndex,
        [FromQuery] int count
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

        var totalCount = await query.CountAsync();

        var dockerImages = await query
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<StarDockerImageResponse>()
        {
            Items = dockerImages,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<ActionResult<StarDockerImageResponse>> GetSingleAsync([FromRoute] int starId, [FromRoute] int id)
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
    public async Task<ActionResult<StarDockerImageResponse>> CreateAsync(
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

        var finalDockerImage = await DockerImageRepository.AddAsync(dockerImage);

        return DockerImageMapper.ToAdminResponse(finalDockerImage);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<ActionResult<StarDockerImageResponse>> UpdateAsync(
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
        await DockerImageRepository.UpdateAsync(dockerImage);

        return DockerImageMapper.ToAdminResponse(dockerImage);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<ActionResult> DeleteAsync([FromRoute] int starId, [FromRoute] int id)
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
        
        await DockerImageRepository.RemoveAsync(dockerImage);
        return NoContent();
    }
}