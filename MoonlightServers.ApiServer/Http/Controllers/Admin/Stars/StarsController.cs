using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
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
    public async Task<IPagedData<StarDetailResponse>> Get(
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var count = await StarRepository.Get().CountAsync();

        var items = await StarRepository
            .Get()
            .OrderBy(x => x.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var mappedItems = items
            .Select(StarMapper.ToAdminResponse)
            .ToArray();

        return new PagedData<StarDetailResponse>()
        {
            CurrentPage = page,
            Items = mappedItems,
            PageSize = pageSize,
            TotalItems = count,
            TotalPages = count == 0 ? 0 : count / pageSize
        };
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.read")]
    public async Task<StarDetailResponse> GetSingle([FromRoute] int id)
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("No star with that id found", 404);

        return StarMapper.ToAdminResponse(star);
    }

    [HttpPost]
    [Authorize(Policy = "permissions:admin.servers.stars.create")]
    public async Task<StarDetailResponse> Create([FromBody] CreateStarRequest request)
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
    public async Task<StarDetailResponse> Update(
        [FromRoute] int id,
        [FromBody] UpdateStarRequest request
    )
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("No star with that id found", 404);
        
        StarMapper.Merge(request, star);
        await StarRepository.Update(star);
        
        return StarMapper.ToAdminResponse(star);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "permissions:admin.servers.stars.delete")]
    public async Task Delete([FromRoute] int id)
    {
        var star = await StarRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("No star with that id found", 404);
        
        await StarRepository.Remove(star);
    }
}