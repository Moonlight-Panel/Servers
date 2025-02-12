using Docker.DotNet;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    private async Task Create()
    {
        var dockerImageService = ServiceProvider.GetRequiredService<DockerImageService>();

        // We call an external service for that, as we want to have a central management point of images
        // for analytics and automatic deletion
        await dockerImageService.Ensure(Configuration.DockerImage, async message => { await LogToConsole(message); });

        var hostPath = await EnsureRuntimeVolume();

        await LogToConsole("Creating container");

        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        var parameters = Configuration.ToRuntimeCreateParameters(
            hostPath: hostPath,
            containerName: RuntimeContainerName
        );

        var container = await dockerClient.Containers.CreateContainerAsync(parameters);
        RuntimeContainerId = container.ID;
    }

    private async Task ReCreate()
    {
        await Destroy();
        await Create();
    }
}