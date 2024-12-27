using Docker.DotNet;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerCreateExtensions
{
    public static async Task Create(this Server server)
    {
        var dockerClient = server.ServiceProvider.GetRequiredService<DockerClient>();

        // Ensure image is pulled
        await server.EnsureDockerImage();
        
        // Ensure runtime storage is created
        await server.EnsureRuntimeStorage();
        
        // Creating container
        await server.NotifyTask(ServerTask.CreatingContainer);
        
        var parameters = server.GetRuntimeContainerParameters();
        var container = await dockerClient.Containers.CreateContainerAsync(parameters);
        
        server.ContainerId = container.ID;
        
        // Attach console
        await server.Attach();
    }

    public static async Task ReCreate(this Server server)
    {
        await server.Destroy();
        await server.Create();
    }
}