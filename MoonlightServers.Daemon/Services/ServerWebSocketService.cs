using Microsoft.AspNetCore.SignalR;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Http.Hubs;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerWebSocketService
{
    private readonly ILogger<ServerWebSocketService> Logger;
    private readonly IServiceProvider ServiceProvider;

    private readonly Dictionary<string, ServerWebSocketConnection> Connections = new();

    public ServerWebSocketService(
        ILogger<ServerWebSocketService> logger,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        ServiceProvider = serviceProvider;
    }

    public async Task InitializeClient(HubCallerContext context)
    {
        var connection = new ServerWebSocketConnection(
            ServiceProvider.GetRequiredService<ServerService>(),
            ServiceProvider.GetRequiredService<ILogger<ServerWebSocketConnection>>(),
            ServiceProvider.GetRequiredService<AccessTokenHelper>(),
            ServiceProvider.GetRequiredService<IHubContext<ServerWebSocketHub>>()
        );

        lock (Connections)
            Connections[context.ConnectionId] = connection;

        await connection.Initialize(context);
    }

    public async Task AuthenticateClient(HubCallerContext context, string accessToken)
    {
        ServerWebSocketConnection? connection;

        lock (Connections)
            connection = Connections.GetValueOrDefault(context.ConnectionId);
        
        if(connection == null)
            return;

        await connection.Authenticate(context, accessToken);
    }

    public async Task DestroyClient(HubCallerContext context)
    {
        ServerWebSocketConnection? connection;

        lock (Connections)
            connection = Connections.GetValueOrDefault(context.ConnectionId);
        
        if(connection == null)
            return;

        await connection.Destroy(context);

        lock (Connections)
            Connections.Remove(context.ConnectionId);
    }
}