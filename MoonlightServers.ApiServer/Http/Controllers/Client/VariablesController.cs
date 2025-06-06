using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Variables;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class VariablesController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly ServerAuthorizeService AuthorizeService;

    public VariablesController(
        DatabaseRepository<Server> serverRepository,
        ServerAuthorizeService authorizeService
    )
    {
        ServerRepository = serverRepository;
        AuthorizeService = authorizeService;
    }

    [HttpGet("{serverId:int}/variables")]
    public async Task<ServerVariableDetailResponse[]> Get([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId, ServerPermissionType.Read);

        return server.Star.Variables.Select(starVariable =>
        {
            var serverVariable = server.Variables.First(x => x.Key == starVariable.Key);

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

    [HttpPut("{serverId:int}/variables")]
    public async Task<ServerVariableDetailResponse> UpdateSingle(
        [FromRoute] int serverId,
        [FromBody] UpdateServerVariableRequest request
    )
    {
        // TODO: Handle filter

        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        var serverVariable = server.Variables.FirstOrDefault(x => x.Key == request.Key);
        var starVariable = server.Star.Variables.FirstOrDefault(x => x.Key == request.Key);

        if (serverVariable == null || starVariable == null)
            throw new HttpApiException($"No variable with the key found: {request.Key}", 400);

        serverVariable.Value = request.Value;
        await ServerRepository.Update(server);

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

    [HttpPatch("{serverId:int}/variables")]
    public async Task<ServerVariableDetailResponse[]> Update(
        [FromRoute] int serverId,
        [FromBody] UpdateServerVariableRangeRequest request
    )
    {
        var server = await GetServerById(serverId, ServerPermissionType.ReadWrite);

        foreach (var variable in request.Variables)
        {
            // TODO: Handle filter

            var serverVariable = server.Variables.FirstOrDefault(x => x.Key == variable.Key);
            var starVariable = server.Star.Variables.FirstOrDefault(x => x.Key == variable.Key);

            if (serverVariable == null || starVariable == null)
                throw new HttpApiException($"No variable with the key found: {variable.Key}", 400);

            serverVariable.Value = variable.Value;
        }

        await ServerRepository.Update(server);

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

    private async Task<Server> GetServerById(int serverId, ServerPermissionType type)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Variables)
            .Include(x => x.Star)
            .ThenInclude(x => x.Variables)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var authorizeResult = await AuthorizeService.Authorize(
            User, server,
            permission => permission.Name == "variables" && permission.Type >= type
        );

        if (!authorizeResult.Succeeded)
            throw new HttpApiException("No permission for the requested resource", 403);

        return server;
    }
}