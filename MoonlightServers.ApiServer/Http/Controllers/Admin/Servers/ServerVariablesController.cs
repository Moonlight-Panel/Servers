using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
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

    public ServerVariablesController(DatabaseRepository<ServerVariable> variableRepository,
        DatabaseRepository<Server> serverRepository)
    {
        VariableRepository = variableRepository;
        ServerRepository = serverRepository;
    }

    [HttpGet("{serverId}/variables")]
    [Authorize(Policy = "permissions:admin.servers.read")]
    public async Task<PagedData<ServerVariableResponse>> Get(
        [FromRoute] int serverId,
        [FromQuery] [Range(0, int.MaxValue)] int page,
        [FromQuery] [Range(1, 100)] int pageSize
    )
    {
        var serverExists = await ServerRepository
            .Get()
            .AnyAsync(x => x.Id == serverId);

        if (!serverExists)
            throw new HttpApiException("No server with this id found", 404);

        var variables = await VariableRepository
            .Get()
            .Where(x => x.Server.Id == serverId)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync();

        var castedVariables = variables
            .Select(ServerVariableMapper.ToAdminResponse)
            .ToArray();

        return PagedData<ServerVariableResponse>.Create(castedVariables, page, pageSize);
    }
}