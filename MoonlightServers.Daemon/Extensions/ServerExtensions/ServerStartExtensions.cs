using Docker.DotNet;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerStartExtensions
{
    public static async Task StateMachineHandler_Start(this Server server)
    {
        await server.ReCreate();

        await server.NotifyTask(ServerTask.StartingContainer);
        var dockerClient = server.ServiceProvider.GetRequiredService<DockerClient>();
        
        await dockerClient.Containers.StartContainerAsync(server.ContainerId, new());
    }
}