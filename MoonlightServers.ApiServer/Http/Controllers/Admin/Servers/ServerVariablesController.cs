using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Extended.Models;
using MoonCore.Models;
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
    public async Task<ActionResult<PagedData<ServerVariableResponse>>> Get(
        [FromRoute] int serverId,
        [FromQuery] PagedOptions options
    )
    {
        var serverExists = await ServerRepository
            .Get()
            .AnyAsync(x => x.Id == serverId);

        if (!serverExists)
            return Problem("No server with this id found", statusCode: 404);

        var query = VariableRepository
            .Get()
            .Where(x => x.Server.Id == serverId);

        var count = await query.CountAsync();

        var variables = await query
            .OrderBy(x => x.Id)
            .Skip(options.Page * options.PageSize)
            .Take(options.PageSize)
            .AsNoTracking()
            .ProjectToAdminResponse()
            .ToArrayAsync();

        return new PagedData<ServerVariableResponse>()
        {
            Items = variables,
            CurrentPage = options.Page,
            PageSize = options.PageSize,
            TotalItems = count,
            TotalPages = (int)Math.Ceiling(Math.Max(0, count) / (double)options.PageSize)
        };
    }
}