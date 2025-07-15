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
[Route("api/admin/servers/stars")]
public class StarDockerImagesController : Controller
{
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<StarDockerImage> StarDockerImageRepository;

    public StarDockerImagesController(
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<StarDockerImage> starDockerImageRepository
    )
    {
        StarRepository = starRepository;
        StarDockerImageRepository = starDockerImageRepository;
    }

    [HttpGet("{starId:int}/dockerImages")]
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
        
        if(starExists)
            throw new HttpApiException("No star with this id found", 404);

        var query = StarDockerImageRepository
            .Get()
            .Where(x => x.Star.Id == starId);

        var count = await query.CountAsync();
        
        var items = await query
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

    [HttpGet("{starId:int}/dockerImages/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<StarDockerImageDetailResponse> GetSingle([FromRoute] int starId, [FromRoute] int id)
    {
        await ApplyStar(starId);

        return await CrudHelper.GetSingle(id);
    }

    [HttpPost("{starId:int}/dockerImages")]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<StarDockerImageDetailResponse> Create([FromRoute] int starId,
        [FromBody] CreateStarDockerImageRequest request)
    {
        await ApplyStar(starId);

        var starDockerImage = Mapper.Map<StarDockerImage>(request);
        starDockerImage.Star = Star;

        var finalVariable = await StarDockerImageRepository.Add(starDockerImage);

        return CrudHelper.MapToResult(finalVariable);
    }

    [HttpPatch("{starId:int}/dockerImages/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.update")]
    public async Task<StarDockerImageDetailResponse> Update([FromRoute] int starId, [FromRoute] int id,
        [FromBody] UpdateStarDockerImageRequest request)
    {
        await ApplyStar(starId);

        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{starId:int}/dockerImages/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.delete")]
    public async Task Delete([FromRoute] int starId, [FromRoute] int id)
    {
        await ApplyStar(starId);

        await CrudHelper.Delete(id);
    }
}