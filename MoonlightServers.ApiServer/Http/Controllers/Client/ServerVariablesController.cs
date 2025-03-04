using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Services;
using MoonlightServers.Shared.Http.Requests.Client.Servers.Variables;
using MoonlightServers.Shared.Http.Responses.Client.Servers.Variables;

namespace MoonlightServers.ApiServer.Http.Controllers.Client;

[Authorize]
[ApiController]
[Route("api/client/servers")]
public class ServerVariablesController : Controller
{
    private readonly DatabaseRepository<Server> ServerRepository;
    private readonly DatabaseRepository<User> UserRepository;
    private readonly ServerService ServerService;

    public ServerVariablesController(
        DatabaseRepository<Server> serverRepository,
        DatabaseRepository<User> userRepository,
        ServerService serverService
    )
    {
        ServerRepository = serverRepository;
        UserRepository = userRepository;
        ServerService = serverService;
    }

    [HttpGet("{serverId:int}/variables")]
    public async Task<ServerVariableDetailResponse[]> Get([FromRoute] int serverId)
    {
        var server = await GetServerById(serverId);

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
    public async Task<ServerVariableDetailResponse> UpdateSingle([FromRoute] int serverId, [FromBody] UpdateServerVariableRequest request)
    {
        // TODO: Handle filter
        
        var server = await GetServerById(serverId);

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
    public async Task<ServerVariableDetailResponse[]> Update([FromRoute] int serverId, [FromBody] UpdateServerVariableRangeRequest request)
    {
        var server = await GetServerById(serverId);

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
                
            };
        }).ToArray();
    }

    private async Task<Server> GetServerById(int serverId)
    {
        var server = await ServerRepository
            .Get()
            .Include(x => x.Variables)
            .Include(x => x.Star)
            .ThenInclude(x => x.Variables)
            .FirstOrDefaultAsync(x => x.Id == serverId);

        if (server == null)
            throw new HttpApiException("No server with this id found", 404);

        var userIdClaim = User.Claims.First(x => x.Type == "userId");
        var userId = int.Parse(userIdClaim.Value);
        var user = await UserRepository.Get().FirstAsync(x => x.Id == userId);

        if (!ServerService.IsAllowedToAccess(user, server))
            throw new HttpApiException("No server with this id found", 404);

        return server;
    }
}