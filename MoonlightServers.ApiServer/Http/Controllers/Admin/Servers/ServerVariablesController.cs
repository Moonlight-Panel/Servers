using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Extended.PermFilter;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.Shared.Http.Responses.Admin.ServerVariables;

namespace MoonlightServers.ApiServer.Http.Controllers.Admin.Servers;

[ApiController]
[Route("api/admin/servers")]
public class ServerVariablesController : Controller
{
    private readonly DatabaseRepository<ServerVariable> VariableRepository;
    private readonly DatabaseRepository<Server> ServerRepository;

    public ServerVariablesController(DatabaseRepository<ServerVariable> variableRepository, DatabaseRepository<Server> serverRepository)
    {
        VariableRepository = variableRepository;
        ServerRepository = serverRepository;
    }

    [HttpGet("{serverId}/variables")]
    [RequirePermission("admin.servers.get")]
    public async Task<PagedData<ServerVariableDetailResponse>> Get([FromRoute] int serverId, [FromQuery] int page, [FromQuery] int pageSize)
    {
        var server = await ServerRepository
            .Get()
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);
        
        var variables = await VariableRepository
            .Get()
            .Where(x => x.Server.Id == server.Id)
            .ToArrayAsync();

        var castedVariables = variables
            .Select(x => Mapper.Map<ServerVariableDetailResponse>(x))
            .ToArray();

        return PagedData<ServerVariableDetailResponse>.Create(castedVariables, page, pageSize);
    }
}