using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Http.Hubs;

public class ServerWebSocketHub : Hub
{
    private readonly ILogger<ServerWebSocketHub> Logger;
    private readonly ServerWebSocketService WebSocketService;

    public ServerWebSocketHub(ILogger<ServerWebSocketHub> logger, ServerWebSocketService webSocketService)
    {
        Logger = logger;
        WebSocketService = webSocketService;
    }

    #region Connection Handlers

    public override async Task OnConnectedAsync()
        => await WebSocketService.InitializeClient(Context);

    public override async Task OnDisconnectedAsync(Exception? exception)
        => await WebSocketService.DestroyClient(Context);

    #endregion

    #region Methods

    [HubMethodName("Authenticate")]
    public async Task Authenticate(string accessToken)
    {
        try
        {
            await WebSocketService.AuthenticateClient(Context, accessToken);
        }
        catch (Exception e)
        {
            Logger.LogError("An unhandled error occured in the Authenticate method: {e}", e);
        }
    }

    #endregion
}