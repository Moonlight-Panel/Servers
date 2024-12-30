using Microsoft.AspNetCore.SignalR;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Http.Hubs;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerConsoleService
{
    private readonly Dictionary<int, ServerConsoleMonitor> Monitors = new();
    private readonly Dictionary<string, ServerConsoleConnection> Connections = new();
    
    private readonly ILogger<ServerConsoleService> Logger;
    private readonly AccessTokenHelper AccessTokenHelper;
    private readonly ServerService ServerService;

    private readonly IHubContext<ServerConsoleHub> HubContext;

    public ServerConsoleService(
        ILogger<ServerConsoleService> logger,
        AccessTokenHelper accessTokenHelper,
        ServerService serverService, IHubContext<ServerConsoleHub> hubContext)
    {
        Logger = logger;
        AccessTokenHelper = accessTokenHelper;
        ServerService = serverService;
        HubContext = hubContext;
    }

    public Task OnClientDisconnected(HubCallerContext context)
    {
        ServerConsoleConnection? removedConnection;

        lock (Connections)
        {
            removedConnection = Connections.GetValueOrDefault(context.ConnectionId);
            
            if(removedConnection != null)
                Connections.Remove(context.ConnectionId);
        }
        
        // Client never authenticated themselves, nothing to do
        if(removedConnection == null)
            return Task.CompletedTask;
        
        Logger.LogDebug("Authenticated client {id} disconnected", context.ConnectionId);
        
        // Count remaining clients requesting the same resource
        int count;

        lock (Connections)
        {
            count = Connections
                .Values
                .Count(x => x.ServerId == removedConnection.ServerId);
        }
        
        if(count > 0)
            return Task.CompletedTask;

        ServerConsoleMonitor? monitor;

        lock (Monitors)
            monitor = Monitors.GetValueOrDefault(removedConnection.ServerId);
        
        if(monitor == null)
            return Task.CompletedTask;
        
        Logger.LogDebug("Destroying console monitor for server {id}", removedConnection.ServerId);
        monitor.Destroy();

        lock (Monitors)
            Monitors.Remove(removedConnection.ServerId);
        
        return Task.CompletedTask;
    }

    public async Task Authenticate(HubCallerContext context, string accessToken)
    {
        // Validate access token
        if (!AccessTokenHelper.Process(accessToken, out var accessData))
        {
            Logger.LogDebug("Received invalid or expired access token. Closing connection");
            context.Abort();
            return;
        }

        // Validate access token data
        if (!accessData.ContainsKey("type") || !accessData.ContainsKey("serverId"))
        {
            Logger.LogDebug("Received invalid access token: Required parameters are missing. Closing connection");
            context.Abort();
            return;
        }

        // Validate access token type
        var type = accessData["type"].GetString()!;

        if (type != "console")
        {
            Logger.LogDebug("Received invalid access token: Invalid type '{type}'. Closing connection", type);
            context.Abort();
            return;
        }

        var serverId = accessData["serverId"].GetInt32();
        var server = ServerService.GetServer(serverId);

        if (server == null)
        {
            Logger.LogDebug("Received invalid access token: No server found with the requested id. Closing connection");
            context.Abort();
            return;
        }

        ServerConsoleConnection? connection;

        lock (Connections)
        {
            connection = Connections
                .GetValueOrDefault(context.ConnectionId);
        }

        if (connection == null) // If no existing connection has been found, we create a new one
        {
            connection = new()
            {
                ServerId = server.Configuration.Id,
                AuthenticatedUntil = DateTime.UtcNow.AddMinutes(10)
            };

            lock (Connections)
                Connections.Add(context.ConnectionId, connection);
            
            Logger.LogDebug("Connection {id} authenticated successfully", context.ConnectionId);
        }
        else
            Logger.LogDebug("Connection {id} re-authenticated successfully", context.ConnectionId);

        ServerConsoleMonitor? monitor;
        
        lock (Monitors)
            monitor = Monitors.GetValueOrDefault(server.Configuration.Id);

        if (monitor == null)
        {
            Logger.LogDebug("Initializing console monitor for server {id}", server.Configuration.Id);
            
            monitor = new ServerConsoleMonitor(server, HubContext.Clients);
            monitor.Initialize();

            lock (Monitors)
                Monitors.Add(server.Configuration.Id, monitor);
        }

        await HubContext.Groups.AddToGroupAsync(context.ConnectionId, $"server-{server.Configuration.Id}");
    }
}