using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MoonlightServers.Daemon.Http.Hubs;

[Authorize(AuthenticationSchemes = "accessToken", Policy = "serverWebsocket")]
public class ServerWebSocketHub : Hub
{
    private readonly ILogger<ServerWebSocketHub> Logger;

    public ServerWebSocketHub(ILogger<ServerWebSocketHub> logger)
    {
        Logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // The policies validated already the type and the token so we can assume we are authenticated
        // and just start adding ourselves into the desired group
        
        var serverId = Context.User!.Claims.First(x => x.Type == "serverId").Value;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            serverId
        );
    }
}