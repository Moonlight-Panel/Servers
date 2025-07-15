using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonCore.Helpers;
using Moonlight.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class ServerService
{
    private readonly NodeService NodeService;
    private readonly DatabaseRepository<Server> ServerRepository;

    public ServerService(NodeService nodeService, DatabaseRepository<Server> serverRepository)
    {
        NodeService = nodeService;
        ServerRepository = serverRepository;
    }

    #region Power Actions

    public async Task Start(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Post($"api/servers/{server.Id}/start");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }
    
    public async Task Stop(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Post($"api/servers/{server.Id}/stop");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }
    
    public async Task Kill(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Post($"api/servers/{server.Id}/kill");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    #endregion
    
    public async Task Install(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Post($"api/servers/{server.Id}/install");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    public async Task Sync(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Post($"api/servers/{server.Id}/sync");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }
    
    public async Task SyncDelete(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            await apiClient.Delete($"api/servers/{server.Id}");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }
    
    public async Task<ServerStatusResponse> GetStatus(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            return await apiClient.GetJson<ServerStatusResponse>($"api/servers/{server.Id}/status");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    public async Task<ServerLogsResponse> GetLogs(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            return await apiClient.GetJson<ServerLogsResponse>($"api/servers/{server.Id}/logs");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    public async Task<ServerStatsResponse> GetStats(Server server)
    {
        try
        {
            using var apiClient = await GetApiClient(server);
            return await apiClient.GetJson<ServerStatsResponse>($"api/servers/{server.Id}/stats");
        }
        catch (HttpRequestException e)
        {
            throw new HttpApiException("Unable to access the node the server is running on", 502);
        }
    }

    #region Helpers

    public bool IsAllowedToAccess(User user, Server server)
    {
        if (server.OwnerId == user.Id)
            return true;
        
        return PermissionHelper.HasPermission(user.Permissions, "admin.servers.get");
    }
    
    private async Task<HttpApiClient> GetApiClient(Server server)
    {
        var serverWithNode = server;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // It can be null when its not included when loading via ef !!!
        if (server.Node == null)
        {
            serverWithNode = await ServerRepository
                .Get()
                .Include(x => x.Node)
                .FirstAsync(x => x.Id == server.Id);
        }

        return NodeService.CreateApiClient(serverWithNode.Node);
    }

    #endregion
}