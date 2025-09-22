using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Requests.Admin.StarVariables;
using MoonlightServers.Shared.Http.Responses.Admin.StarVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars/{starId:int}/variables")]
public class StarVariablesController : Controller
{
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly DatabaseRepository<StarVariable> VariableRepository;

    public StarVariablesController(
        DatabaseRepository<Star> starRepository,
        DatabaseRepository<StarVariable> variableRepository)
    {
        StarRepository = starRepository;
        VariableRepository = variableRepository;
    }

    [HttpGet]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<ActionResult<CountedData<StarVariableResponse>>> GetAsync(
        [FromRoute] int starId,
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            return Problem("No star with this id found", statusCode: 404);

        var query = VariableRepository
            .Get()
            .Where(x => x.Star.Id == starId);

        var totalCount = await query.CountAsync();

        var variables = await query
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<StarVariableResponse>()
        {
            Items = variables,
            TotalCount = totalCount
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<StarVariableResponse> GetSingleAsync(
        [FromRoute] int starId,
        [FromRoute] int id
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var starVariable = await VariableRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (starVariable == null)
            throw new HttpApiException("No variable with this id found", 404);

        return StarVariableMapper.ToAdminResponse(starVariable);
    }

    [HttpPost("")]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<StarVariableResponse> CreateAsync([FromRoute] int starId,
        [FromBody] CreateStarVariableRequest request)
    {
        var star = StarRepository
            .Get()
            .FirstOrDefault(x => x.Id == starId);

        if (star == null)
            throw new HttpApiException("No star with this id found", 404);

        var starVariable = StarVariableMapper.ToStarVariable(request);
        starVariable.Star = star;

        await VariableRepository.AddAsync(starVariable);

        return StarVariableMapper.ToAdminResponse(starVariable);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.update")]
    public async Task<StarVariableResponse> UpdateAsync(
        [FromRoute] int starId,
        [FromRoute] int id,
        [FromBody] UpdateStarVariableRequest request
    )
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var starVariable = await VariableRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (starVariable == null)
            throw new HttpApiException("No variable with this id found", 404);
        
        StarVariableMapper.Merge(request, starVariable);
        await VariableRepository.UpdateAsync(starVariable);
        
        return StarVariableMapper.ToAdminResponse(starVariable);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.delete")]
    public async Task DeleteAsync([FromRoute] int starId, [FromRoute] int id)
    {
        var starExists = StarRepository
            .Get()
            .Any(x => x.Id == starId);

        if (!starExists)
            throw new HttpApiException("No star with this id found", 404);

        var starVariable = await VariableRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id && x.Star.Id == starId);

        if (starVariable == null)
            throw new HttpApiException("No variable with this id found", 404);
        
        await VariableRepository.RemoveAsync(starVariable);
    }
}