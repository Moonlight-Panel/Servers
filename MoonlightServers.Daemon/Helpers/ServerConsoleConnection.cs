using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Helpers;

public class ServerConsoleConnection
{
    private readonly ServerService ServerService;
    private readonly ILogger<ServerConsoleConnection> Logger;
    private readonly AccessTokenHelper AccessTokenHelper;
    private readonly IHubContext<ServerConsoleHub> HubContext;

    private int ServerId = -1;
    private Server Server;
    private bool IsInitialized = false;
    private string ConnectionId;

    public ServerConsoleConnection(
        ServerService serverService,
        ILogger<ServerConsoleConnection> logger,
        AccessTokenHelper accessTokenHelper,
        IHubContext<ServerConsoleHub> hubContext
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
        if (!accessData.ContainsKey("type") || !accessData.ContainsKey("serverId"))
        {
            Logger.LogDebug("Received invalid access token: Required parameters are missing");
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                "Received invalid access token: Required parameters are missing"
            );
            
            return;
        }

        // Validate access token type
        var type = accessData["type"].GetString()!;

        if (type != "console")
        {
            Logger.LogDebug("Received invalid access token: Invalid type '{type}'", type);
            
            await HubContext.Clients.Client(context.ConnectionId).SendAsync(
                "Error",
                $"Received invalid access token: Invalid type '{type}'"
            );
            
            return;
        }

        var serverId = accessData["serverId"].GetInt32();
        
        // Check that the access token isn't or another server
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
        Server.StateMachine.OnTransitioned += HandlePowerStateChange;
        Server.OnTaskAdded += HandleTaskAdded;
        Server.Console.OnOutput += HandleConsoleOutput;
        
        Logger.LogTrace("Authenticated and initialized server console connection '{id}'", context.ConnectionId);
    }

    public Task Destroy(HubCallerContext context)
    {
        Server.StateMachine.OnTransitioned -= HandlePowerStateChange;
        Server.OnTaskAdded -= HandleTaskAdded;
        
        Logger.LogTrace("Destroyed server console connection '{id}'", context.ConnectionId);
        
        return Task.CompletedTask;
    }

    #region Event Handlers

    private async Task HandlePowerStateChange(ServerState serverState)
     => await HubContext.Clients.Client(ConnectionId).SendAsync("PowerStateChanged", serverState.ToString());
    
    private async Task HandleTaskAdded(string task)
        => await HubContext.Clients.Client(ConnectionId).SendAsync("TaskNotify", task);

    private async Task HandleConsoleOutput(string line)
        => await HubContext.Clients.Client(ConnectionId).SendAsync("ConsoleOutput", line);

    #endregion
}