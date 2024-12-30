using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Http.Hubs;

public class ServerConsoleHub : Hub
{
    private readonly ILogger<ServerConsoleHub> Logger;
    private readonly ServerConsoleService ConsoleService;

    public ServerConsoleHub(ILogger<ServerConsoleHub> logger, ServerConsoleService consoleService)
    {
        Logger = logger;
        ConsoleService = consoleService;
    }

    [HubMethodName("Authenticate")]
    public async Task Authenticate(string accessToken)
    {
        try
        {
            await ConsoleService.Authenticate(Context, accessToken);
        }
        catch (Exception e)
        {
            Logger.LogError("An unhandled error occured in the Authenticate method: {e}", e);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
        => await ConsoleService.OnClientDisconnected(Context);
}