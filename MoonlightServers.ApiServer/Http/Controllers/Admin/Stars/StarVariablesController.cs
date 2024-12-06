using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.StarVariables;
using MoonlightServers.Shared.Http.Responses.Admin.StarVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars")]
public class StarVariablesController : Controller
{
    private readonly CrudHelper<StarVariable, StarVariableDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<StarVariable> StarVariableRepository;

    private Star Star;

    public StarVariablesController(
        CrudHelper<StarVariable, StarVariableDetailResponse> crudHelper,
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<StarVariable> starVariableRepository)
    {
        CrudHelper = crudHelper;
        StarRepository = starRepository;
        StarVariableRepository = starVariableRepository;
    }

    private void ApplyStar(int id)
    {
        var star = StarRepository
            .Get()
            .FirstOrDefault(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("A star with this id could not be found", 404);

        Star = star;

        CrudHelper.QueryModifier = variables =>
            variables.Where(x => x.Star.Id == star.Id);
    }

    [HttpGet("{starId:int}/variables")]
    [RequirePermission("admin.servers.stars.get")]
    public async Task<IPagedData<StarVariableDetailResponse>> Get([FromRoute] int starId, [FromQuery] int page, [FromQuery] int pageSize)
    {
        ApplyStar(starId);

        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{starId:int}/variables/{id:int}")]
    [RequirePermission("admin.servers.stars.get")]
    public async Task<StarVariableDetailResponse> GetSingle([FromRoute] int starId, [FromRoute] int id)
    {
        ApplyStar(starId);
        
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost("{starId:int}/variables")]
    [RequirePermission("admin.servers.stars.create")]
    public async Task<StarVariableDetailResponse> Create([FromRoute] int starId, [FromBody] CreateStarVariableRequest request)
    {
        ApplyStar(starId);
        
        var starVariable = Mapper.Map<StarVariable>(request);
        starVariable.Star = Star;

        var finalVariable = StarVariableRepository.Add(starVariable);

        return CrudHelper.MapToResult(finalVariable);
    }

    [HttpPatch("{starId:int}/variables/{id:int}")]
    [RequirePermission("admin.servers.stars.update")]
    public async Task<StarVariableDetailResponse> Update([FromRoute] int starId, [FromRoute] int id,
        [FromBody] UpdateStarVariableRequest request)
    {
        ApplyStar(starId);
        
        var variable = await CrudHelper.GetSingleModel(id);

        variable = Mapper.Map(variable, request);
        
        StarVariableRepository.Update(variable);
        
        return CrudHelper.MapToResult(variable);
    }

    [HttpDelete("{starId:int}/variables/{id:int}")]
    [RequirePermission("admin.servers.stars.delete")]
    public async Task Delete([FromRoute] int starId, [FromRoute] int id)
    {
        ApplyStar(starId);
        
        await CrudHelper.Delete(id);
    }
}