using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Common;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Mappers;
using MoonlightServers.Shared.Http.Responses.Admin.ServerVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Servers;

[ApiController]
[Route("api/admin/servers")]
public class ServerVariablesController : Controller
{
    private readonly DatabaseRepository<ServerVariable> VariableRepository;
    private readonly DatabaseRepository<Server> ServerRepository;

    public ServerVariablesController(
        DatabaseRepository<ServerVariable> variableRepository,
        DatabaseRepository<Server> serverRepository
    )
    {
        VariableRepository = variableRepository;
        ServerRepository = serverRepository;
    }

    [HttpGet("{serverId:int}/variables")]
    [Authorize(Policy = "permissions:admin.servers.read")]
    public async Task<ActionResult<CountedData<ServerVariableResponse>>> GetAsync(
        [FromRoute] int serverId,
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var serverExists = await ServerRepository
            .Get()
            .AnyAsync(x => x.Id == serverId);

        if (!serverExists)
            return Problem("No server with this id found", statusCode: 404);

        var query = VariableRepository
            .Get()
            .Where(x => x.Server.Id == serverId);

        var totalCount = await query.CountAsync();

        var variables = await query
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new CountedData<ServerVariableResponse>()
        {
            Items = variables,
            TotalCount = totalCount
        };
    }
}