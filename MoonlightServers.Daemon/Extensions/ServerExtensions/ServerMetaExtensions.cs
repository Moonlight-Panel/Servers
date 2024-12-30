using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerMetaExtensions
{
    public static async Task NotifyTask(this Server server, ServerTask task)
    {
        server.Logger.LogInformation("Task: {task}", task);
        await server.InvokeTaskAdded(task.ToString());
    }
}