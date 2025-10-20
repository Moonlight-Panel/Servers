using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Common;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Constants;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Variables;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers/{serverId:int}/variables")]
public class VariablesController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<ServerVariable> ServerVariableRepository;
    private readonly DatabaseRepository<StarVariable> StarVariableRepository;
    private readonly ServerAuthorizeService AuthorizeService;

    public VariablesController(
        DatabaseRepository<Server> serverRepository,
        ServerAuthorizeService authorizeService,
        DatabaseRepository<ServerVariable> serverVariableRepository,
        DatabaseRepository<StarVariable> starVariableRepository
    )
    {
        ServerRepository = serverRepository;
        AuthorizeService = authorizeService;
        ServerVariableRepository = serverVariableRepository;
        StarVariableRepository = starVariableRepository;
    }

    [HttpGet]
    public async Task<ActionResult<CountedData<ServerVariableDetailResponse>>> GetAsync(
        [FromRoute] int serverId,
        [FromQuery] int startIndex,
        [FromQuery] int count
    )
    {
        if (count > 100)
            return Problem("Only 100 items can be fetched at a time", statusCode: 400);
        
        var server = await GetServerByIdAsync(serverId, ServerPermissionLevel.Read);
        
        if (server.Value == null)
            return server.Result ?? Problem("Unable to retrieve server");

        var query = StarVariableRepository
            .Get()
            .Where(x => x.Star.Id == server.Value.Star.Id);

        var totalCount = await query.CountAsync();

        var starVariables = await query
            .OrderBy(x => x.Id)
            .Skip(startIndex)
            .Take(count)
            .ToArrayAsync();

        var starVariableKeys = starVariables
            .Select(x => x.Key)
            .ToArray();

        var serverVariables = await ServerVariableRepository
            .Get()
            .Where(x => x.Server.Id == server.Value.Id && starVariableKeys.Contains(x.Key))
            .ToArrayAsync();

        var responses = starVariables.Select(starVariable =>
        {
            var serverVariable = serverVariables.First(x => x.Key == starVariable.Key);

            return new ServerVariableDetailResponse()
            {
                Key = starVariable.Key,
                Value = serverVariable.Value,
                Type = starVariable.Type,
                Name = starVariable.Name,
                Description = starVariable.Description,
                Filter = starVariable.Filter
            };
        }).ToArray();

        return new CountedData<ServerVariableDetailResponse>()
        {
            Items = responses,
            TotalCount = totalCount
        };
    }

    [HttpPut]
    public async Task<ActionResult<ServerVariableDetailResponse>> UpdateSingleAsync(
        [FromRoute] int serverId,
        [FromBody] UpdateServerVariableRequest request
    )
    {
        // TODO: Handle filter

        var serverResult = await GetServerByIdAsync(serverId, ServerPermissionLevel.ReadWrite);
        
        if (serverResult.Value == null)
            return serverResult.Result ?? Problem("Unable to retrieve server");

        var server = serverResult.Value;

        var serverVariable = server.Variables.FirstOrDefault(x => x.Key == request.Key);
        var starVariable = server.Star.Variables.FirstOrDefault(x => x.Key == request.Key);

        if (serverVariable == null || starVariable == null)
            throw new HttpApiException($"No variable with the key found: {request.Key}", 400);

        serverVariable.Value = request.Value;
        await ServerRepository.UpdateAsync(server);

        return new ServerVariableDetailResponse()
        {
            Key = starVariable.Key,
            Value = serverVariable.Value,
            Type = starVariable.Type,
            Name = starVariable.Name,
            Description = starVariable.Description,
            Filter = starVariable.Filter
        };
    }

    [HttpPatch]
    public async Task<ActionResult<ServerVariableDetailResponse[]>> UpdateAsync(
        [FromRoute] int serverId,
        [FromBody] UpdateServerVariableRangeRequest request
    )
    {
        var serverResult = await GetServerByIdAsync(serverId, ServerPermissionLevel.ReadWrite);
        
        if (serverResult.Value == null)
            return serverResult.Result ?? Problem("Unable to retrieve server");

        var server = serverResult.Value;

        foreach (var variable in request.Variables)
        {
            // TODO: Handle filter

            var serverVariable = server.Variables.FirstOrDefault(x => x.Key == variable.Key);
            var starVariable = server.Star.Variables.FirstOrDefault(x => x.Key == variable.Key);

            if (serverVariable == null || starVariable == null)
                throw new HttpApiException($"No variable with the key found: {variable.Key}", 400);

            serverVariable.Value = variable.Value;
        }

        await ServerRepository.UpdateAsync(server);

        return request.Variables.Select(requestVariable =>
        {
            var serverVariable = server.Variables.First(x => x.Key == requestVariable.Key);
            var starVariable = server.Star.Variables.First(x => x.Key == requestVariable.Key);

            return new ServerVariableDetailResponse()
            {
                Key = starVariable.Key,
                Value = serverVariable.Value,
                Type = starVariable.Type,
                Name = starVariable.Name,
                Description = starVariable.Description,
                Filter = starVariable.Filter
            };
        }).ToArray();
    }

    private async Task<ActionResult<Server>> GetServerByIdAsync(int serverId, ServerPermissionLevel level)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Star)
            .ThenInclude(x => x.Variables)
            .Include(x => x.Variables)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            return Problem("No server with this id found", statusCode: 404);

        var authorizeResult = await AuthorizeService.AuthorizeAsync(
            User, server,
            ServerPermissionConstants.Variables,
            level
        );

        if (!authorizeResult.Succeeded)
        {
            return Problem(
                authorizeResult.Message ?? "No permission for the requested resource",
                statusCode: 403
            );
        }

        return server;
    }
}