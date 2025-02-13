using Docker.DotNet;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    private async Task Destroy()
    {
        // Note: This only destroys the container, it doesn't delete any data

        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();
        
        try
        {
            var container = await dockerClient.Containers.InspectContainerAsync(
                RuntimeContainerName
            );

            if (container.State.Running)
            {
                // Stop container when running
                
                await LogToConsole("Stopping container");
                
                await dockerClient.Containers.StopContainerAsync(container.ID, new()
                {
                    WaitBeforeKillSeconds = 30 // TODO: Config
                });
            }

            await LogToConsole("Removing container");
            await dockerClient.Containers.RemoveContainerAsync(container.ID, new());

            RuntimeContainerId = null;
        }
        catch (DockerContainerNotFoundException){}
        
        // Canceling server tasks & listeners
        await CancelTasks();
        
        // and recreating cancellation token
        Cancellation = new();
    }

    public async Task CancelTasks()
    {
        // Note: This will keep the docker container running, it will just cancel the server cancellation token
        
        if (!Cancellation.IsCancellationRequested)
            await Cancellation.CancelAsync();
    }
}