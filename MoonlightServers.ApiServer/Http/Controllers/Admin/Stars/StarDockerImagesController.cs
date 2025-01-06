using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars")]
public class StarDockerImagesController : Controller
{
    private readonly CrudHelper<StarDockerImage, StarDockerImageDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<StarDockerImage> StarDockerImageRepository;
    
    private Star Star;

    public StarDockerImagesController(
        CrudHelper<StarDockerImage, StarDockerImageDetailResponse> crudHelper,
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<StarDockerImage> starDockerImageRepository
    )
    {
        CrudHelper = crudHelper;
        StarRepository = starRepository;
        StarDockerImageRepository = starDockerImageRepository;
    }
    
    private async Task ApplyStar(int id)
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("A star with this id could not be found", 404);

        Star = star;

        CrudHelper.QueryModifier = dockerImages =>
            dockerImages.Where(x => x.Star.Id == star.Id);
    }

    [HttpGet("{starId:int}/dockerImages")]
    public async Task<IPagedData<StarDockerImageDetailResponse>> Get([FromRoute] int starId, [FromQuery] int page, [FromQuery] int pageSize)
    {
        await ApplyStar(starId);
        
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{starId:int}/dockerImages/{id:int}")]
    public async Task<StarDockerImageDetailResponse> GetSingle([FromRoute] int starId, [FromRoute] int id)
    {
        await ApplyStar(starId);
        
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost("{starId:int}/dockerImages")]
    public async Task<StarDockerImageDetailResponse> Create([FromRoute] int starId, [FromBody] CreateStarDockerImageRequest request)
    {
        await ApplyStar(starId);
        
        var starDockerImage = Mapper.Map<StarDockerImage>(request);
        starDockerImage.Star = Star;

        var finalVariable = await StarDockerImageRepository.Add(starDockerImage);

        return CrudHelper.MapToResult(finalVariable);
    }

    [HttpPatch("{starId:int}/dockerImages/{id:int}")]
    public async Task<StarDockerImageDetailResponse> Update([FromRoute] int starId, [FromRoute] int id,
        [FromBody] UpdateStarDockerImageRequest request)
    {
        await ApplyStar(starId);
        
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{starId:int}/dockerImages/{id:int}")]
    public async Task Delete([FromRoute] int starId, [FromRoute] int id)
    {
        await ApplyStar(starId);
        
        await CrudHelper.Delete(id);
    }
}