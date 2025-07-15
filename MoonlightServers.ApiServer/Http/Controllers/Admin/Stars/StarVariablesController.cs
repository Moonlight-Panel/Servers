using System.ComponentModel.DataAnnotations;
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
[Route("api/admin/servers/stars")]
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

    [HttpGet("{starId:int}/variables")]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<IPagedData<StarVariableDetailResponse>> Get(
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

        var query = VariableRepository
            .Get()
            .Where(x => x.Star.Id == starId);

        var count = await query.CountAsync();

        var items = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var mappedItems = items
            .Select(StarVariableMapper.ToAdminResponse)
            .ToArray();

        return new PagedData<StarVariableDetailResponse>()
        {
            Items = mappedItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{starId:int}/variables/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.get")]
    public async Task<StarVariableDetailResponse> GetSingle(
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

    [HttpPost("{starId:int}/variables")]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<StarVariableDetailResponse> Create([FromRoute] int starId,
        [FromBody] CreateStarVariableRequest request)
    {
        var star = StarRepository
            .Get()
            .FirstOrDefault(x => x.Id == starId);

        if (star == null)
            throw new HttpApiException("No star with this id found", 404);

        var starVariable = StarVariableMapper.ToStarVariable(request);
        starVariable.Star = star;

        await VariableRepository.Add(starVariable);

        return StarVariableMapper.ToAdminResponse(starVariable);
    }

    [HttpPatch("{starId:int}/variables/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.update")]
    public async Task<StarVariableDetailResponse> Update(
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
        
        starVariable = StarVariableMapper.Merge(request, starVariable);
        await VariableRepository.Update(starVariable);
        
        return StarVariableMapper.ToAdminResponse(starVariable);
    }

    [HttpDelete("{starId:int}/variables/{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.delete")]
    public async Task Delete([FromRoute] int starId, [FromRoute] int id)
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
        
        await VariableRepository.Remove(starVariable);
    }
}