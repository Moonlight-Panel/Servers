using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerStorageExtensions
{
    public static async Task EnsureRuntimeStorage(this Server server)
    {
        // TODO: Add virtual disk
        await server.NotifyTask(ServerTask.CreatingStorage);

        // Create volume if missing
        if (!Directory.Exists(server.RuntimeVolumePath))
            Directory.CreateDirectory(server.RuntimeVolumePath);
        
        // TODO: Chown
        //Syscall.chown()
    }
}