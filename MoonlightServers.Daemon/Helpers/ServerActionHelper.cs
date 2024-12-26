using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Models;

namespace MoonlightServers.Daemon.Helpers;

public class ServerActionHelper
{
    public static async Task Start(Server server, IServiceProvider serviceProvider)
    {
        await EnsureStorage(server, serviceProvider);
        await EnsureDockerImage(server, serviceProvider);
        await CreateRuntimeContainer(server, serviceProvider);
        await StartRuntimeContainer(server, serviceProvider);
    }

    private static async Task EnsureStorage(Server server, IServiceProvider serviceProvider)
    {
        await NotifyTask(server, serviceProvider, ServerTask.CreatingStorage);

        // Build paths
        var configuration = serviceProvider.GetRequiredService<AppConfiguration>();

        var volumePath = PathBuilder.Dir(
            configuration.Storage.Volumes,
            server.Configuration.Id.ToString()
        );

        // Create volume if missing
        if (!Directory.Exists(volumePath))
            Directory.CreateDirectory(volumePath);

        // TODO: Virtual disk
    }

    private static async Task EnsureDockerImage(Server server, IServiceProvider serviceProvider)
    {
        await NotifyTask(server, serviceProvider, ServerTask.PullingDockerImage);

        var dockerClient = serviceProvider.GetRequiredService<DockerClient>();

        await dockerClient.Images.CreateImageAsync(new()
            {
                FromImage = server.Configuration.DockerImage
            },
            new AuthConfig(),
            new Progress<JSONMessage>(async message =>
            {
                //var percentage = (int)(message.Progress.Current / message.Progress.Total);
                //await UpdateProgress(server, serviceProvider, percentage);
            })
        );
    }

    private static async Task CreateRuntimeContainer(Server server, IServiceProvider serviceProvider)
    {
        var dockerClient = serviceProvider.GetRequiredService<DockerClient>();

        try
        {
            var existingContainer = await dockerClient.Containers.InspectContainerAsync(
                $"moonlight-runtime-{server.Configuration.Id}"
            );

            await NotifyTask(server, serviceProvider, ServerTask.RemovingContainer);

            if (existingContainer.State.Running) // Stop already running container
            {
                await dockerClient.Containers.StopContainerAsync(existingContainer.ID, new()
                {
                    WaitBeforeKillSeconds = 30 // TODO: Config
                });
            }

            await dockerClient.Containers.RemoveContainerAsync(existingContainer.ID, new());
        }
        catch (DockerContainerNotFoundException)
        {
        }
        
        await NotifyTask(server, serviceProvider, ServerTask.CreatingContainer);

        // Create a new container
        var parameters = new CreateContainerParameters();

        ServerConfigurationHelper.ApplyRuntimeOptions(
            parameters,
            server.Configuration,
            serviceProvider.GetRequiredService<AppConfiguration>()
        );

        var container = await dockerClient.Containers.CreateContainerAsync(parameters);
        server.ContainerId = container.ID;
    }

    private static async Task StartRuntimeContainer(Server server, IServiceProvider serviceProvider)
    {
        await NotifyTask(server, serviceProvider, ServerTask.StartingContainer);
        
        var dockerClient = serviceProvider.GetRequiredService<DockerClient>();

        await dockerClient.Containers.StartContainerAsync(server.ContainerId, new());
    }

    private static async Task NotifyTask(Server server, IServiceProvider serviceProvider, ServerTask task)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger($"Server {server.Configuration.Id}");
        
        logger.LogInformation("Task: {task}", task);
    }

    private static async Task UpdateProgress(Server server, IServiceProvider serviceProvider, int progress)
    {
    }
}