using Docker.DotNet;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerDestroyExtensions
{
    public static async Task Destroy(this Server server)
    {
        // Note: This only destroys the container, it doesn't delete any data

        var dockerClient = server.ServiceProvider.GetRequiredService<DockerClient>();
        
        try
        {
            var container = await dockerClient.Containers.InspectContainerAsync(
                server.RuntimeContainerName
            );

            if (container.State.Running)
            {
                // Stop container when running

                await server.NotifyTask(ServerTask.StoppingContainer);
                
                await dockerClient.Containers.StopContainerAsync(container.ID, new()
                {
                    WaitBeforeKillSeconds = 30 // TODO: Config
                });
            }

            await server.NotifyTask(ServerTask.RemovingContainer);
            await dockerClient.Containers.RemoveContainerAsync(container.ID, new());
        }
        catch (DockerContainerNotFoundException){}
        
        // Canceling server sub-tasks and recreating cancellation token
        if (!server.Cancellation.IsCancellationRequested)
            await server.Cancellation.CancelAsync();

        server.Cancellation = new();
    }
}