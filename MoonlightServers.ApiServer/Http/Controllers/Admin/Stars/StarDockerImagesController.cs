using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<IPagedData<StarDockerImageDetailResponse>> Get(
        [FromRoute] int starId,
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var query = DockerImageRepository
            .Get()
            .Where(x => x.Star.Id == starId);

        var count = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var mappedItems = items
            .Select(DockerImageMapper.ToAdminResponse)
            .ToArray();

        return new PagedData<StarDockerImageDetailResponse>()
        {
            Items = mappedItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<StarDockerImageDetailResponse> GetSingle([FromRoute] int starId, [FromRoute] int id)
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (dockerImage == null)
            throw new HttpApiException("No star docker image with this id found", 404);

        return DockerImageMapper.ToAdminResponse(dockerImage);
    }

    [HttpPost("")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<StarDockerImageDetailResponse> Create(
        [FromRoute] int starId,
        [FromBody] CreateStarDockerImageRequest request
    )
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == starId);

        if (star == null)
            throw new HttpApiException("No star with this id found", 404);

        var dockerImage = DockerImageMapper.ToDockerImage(request);
        dockerImage.Star = star;

        var finalDockerImage = await DockerImageRepository.Add(dockerImage);

        return DockerImageMapper.ToAdminResponse(finalDockerImage);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task<StarDockerImageDetailResponse> Update(
        [FromRoute] int starId,
        [FromRoute] int id,
        [FromBody] UpdateStarDockerImageRequest request
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (dockerImage == null)
            throw new HttpApiException("No star docker image with this id found", 404);

        DockerImageMapper.Merge(request, dockerImage);
        await DockerImageRepository.Update(dockerImage);

        return DockerImageMapper.ToAdminResponse(dockerImage);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.write")]
    public async Task Delete([FromRoute] int starId, [FromRoute] int id)
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var dockerImage = await DockerImageRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (dockerImage == null)
            throw new HttpApiException("No star docker image with this id found", 404);
        
        await DockerImageRepository.Remove(dockerImage);
    }
}