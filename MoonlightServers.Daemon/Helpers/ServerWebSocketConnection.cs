using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Abstractions;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Helpers;

public class ServerWebSocketConnection
{
    private readonly ServerService ServerService;
    private readonly ILogger<ServerWebSocketConnection> Logger;
    private readonly AccessTokenHelper AccessTokenHelper;
    private readonly IHubContext<ServerWebSocketHub> HubContext;

    private int ServerId = -1;
    private Server Server;
    private bool IsInitialized = false;
    private string ConnectionId;

    public ServerWebSocketConnection(
        ServerService serverService,
        ILogger<ServerWebSocketConnection> logger,
        AccessTokenHelper accessTokenHelper,
        IHubContext<ServerWebSocketHub> hubContext
    )
    {
        ServerService = serverService;
        Logger = logger;
        AccessTokenHelper = accessTokenHelper;
        HubContext = hubContext;
    }

    public Task Initialize(HubCallerContext context) => Task.CompletedTask;

    public async Task Authenticate(HubCallerContext context, string accessToken)
    {
        // Validate access token
        if (!AccessTokenHelper.Process(accessToken, out var accessData))
        {
            Logger.LogDebug("Received invalid or expired access token");
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                "Received invalid or expired access token"
            );
            
            return;
        }

        // Validate access token data
        if (accessData.All(x => x.Type != "type") || accessData.All(x => x.Type != "serverId"))
        {
            Logger.LogDebug("Received invalid access token: Required parameters are missing");
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                "Received invalid access token: Required parameters are missing"
            );
            
            return;
        }

        // Validate access token type
        var type = accessData.First(x => x.Type == "type").Value;

        if (type != "websocket")
        {
            Logger.LogDebug("Received invalid access token: Invalid type '{type}'", type);
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                $"Received invalid access token: Invalid type '{type}'"
            );
            
            return;
        }

        var serverId = int.Parse(accessData.First(x => x.Type == "serverId").Value);
        
        // Check that the access token isn't for another server
        if (ServerId != -1 && ServerId == serverId)
        {
            Logger.LogDebug("Received invalid access token: Server id not valid for this session. Current server id: {serverId}", ServerId);
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                $"Received invalid access token: Server id not valid for this session. Current server id: {ServerId}"
            );
            
            return;
        }
        
        var server = ServerService.GetServer(serverId);

        // Check i the server actually exists
        if (server == null)
        {
            Logger.LogDebug("Received invalid access token: No server found with the requested id");
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                "Received invalid access token: No server found with the requested id"
            );
            
            return;
        }

        // Set values
        Server = server;
        ServerId = serverId;
        ConnectionId = context.ConnectionId;
        
        if(IsInitialized)
            return;

        IsInitialized = true;
        
        // Setup event handlers
        Server.OnConsoleOutput += HandleConsoleOutput;
        Server.OnStateChanged += HandleStateChange;
        
        Logger.LogTrace("Authenticated and initialized server console connection '{id}'", context.ConnectionId);
    }

    public Task Destroy(HubCallerContext context)
    {
        Logger.LogTrace("Destroyed server console connection '{id}'", context.ConnectionId);
        
        Server.OnConsoleOutput -= HandleConsoleOutput;
        Server.OnStateChanged -= HandleStateChange;
        
        return Task.CompletedTask;
    }

    #region Event Handlers
    
    private async Task HandleStateChange(ServerState state)
        => await HubContext.Clients.Client(ConnectionId).SendAsync("StateChanged", state.ToString());
    
    private async Task HandleConsoleOutput(string line)
        => await HubContext.Clients.Client(ConnectionId).SendAsync("ConsoleOutput", line);

    #endregion
}