using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Requests.Admin.Stars;
using MoonlightServers.Shared.Http.Responses.Admin.StarDockerImages;
using MoonlightServers.Shared.Http.Responses.Admin.Stars;
using MoonlightServers.Shared.Http.Responses.Admin.StarVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Stars;

[ApiController]
[Route("api/admin/servers/stars")]
public class StarsController : Controller
{
    private readonly CrudHelper<Star, StarDetailResponse> CrudHelper;
    private readonly DatabaseRepository<Star> StarRepository;

    public StarsController(CrudHelper<Star, StarDetailResponse> crudHelper, DatabaseRepository<Star> starRepository)
    {
        CrudHelper = crudHelper;
        StarRepository = starRepository;

        CrudHelper.QueryModifier = stars => stars
            .Include(x => x.Variables)
            .Include(x => x.DockerImages);

        CrudHelper.LateMapper = (star, response) =>
        {
            response.DockerImages = star.DockerImages
                .Select(x => Mapper.Map<StarDockerImageDetailResponse>(x))
                .ToArray();

            response.Variables = star.Variables
                .Select(x => Mapper.Map<StarVariableDetailResponse>(x))
                .ToArray();
            
            return response;
        };
    }

    [HttpGet]
    [RequirePermission("admin.servers.stars.get")]
    public async Task<IPagedData<StarDetailResponse>> Get([FromQuery] int page, [FromQuery] int pageSize)
    {
        return await CrudHelper.Get(page, pageSize);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("admin.servers.stars.get")]
    public async Task<StarDetailResponse> GetSingle([FromRoute] int id)
    {
        return await CrudHelper.GetSingle(id);
    }

    [HttpPost]
    [RequirePermission("admin.servers.stars.create")]
    public async Task<StarDetailResponse> Create([FromBody] CreateStarRequest request)
    {
        var star = Mapper.Map<Star>(request);
        
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
        star.ParseConfiguration = "[]";

        var finalStar = StarRepository.Add(star);

        return CrudHelper.MapToResult(finalStar);
    }

    [HttpPatch("{id:int}")]
    [RequirePermission("admin.servers.stars.update")]
    public async Task<StarDetailResponse> Update([FromRoute] int id, [FromBody] UpdateStarRequest request)
    {
        return await CrudHelper.Update(id, request);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("admin.servers.stars.delete")]
    public async Task Delete([FromRoute] int id)
    {
        await CrudHelper.Delete(id);
    }
}