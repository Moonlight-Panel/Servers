using Microsoft.AspNetCore.SignalR;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Http.Hubs;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ServerConsoleService
{
    private readonly ILogger<ServerConsoleService> Logger;
    private readonly IServiceProvider ServiceProvider;

    private readonly Dictionary<string, ServerConsoleConnection> Connections = new();

    public ServerConsoleService(
        ILogger<ServerConsoleService> logger,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        ServiceProvider = serviceProvider;
    }

    public async Task InitializeClient(HubCallerContext context)
    {
        var connection = new ServerConsoleConnection(
            ServiceProvider.GetRequiredService<ServerService>(),
            ServiceProvider.GetRequiredService<ILogger<ServerConsoleConnection>>(),
            ServiceProvider.GetRequiredService<AccessTokenHelper>(),
            ServiceProvider.GetRequiredService<IHubContext<ServerConsoleHub>>()
        );

        lock (Connections)
            Connections[context.ConnectionId] = connection;

        await connection.Initialize(context);
    }

    public async Task AuthenticateClient(HubCallerContext context, string accessToken)
    {
        ServerConsoleConnection? connection;

        lock (Connections)
            connection = Connections.GetValueOrDefault(context.ConnectionId);
        
        if(connection == null)
            return;

        await connection.Authenticate(context, accessToken);
    }

    public async Task DestroyClient(HubCallerContext context)
    {
        ServerConsoleConnection? connection;

        lock (Connections)
            connection = Connections.GetValueOrDefault(context.ConnectionId);
        
        if(connection == null)
            return;

        await connection.Destroy(context);

        lock (Connections)
            Connections.Remove(context.ConnectionId);
    }
}