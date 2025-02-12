using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Attributes;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class DockerImageService
{
    private readonly DockerClient DockerClient;
    private readonly ILogger<DockerImageService> Logger;

    public DockerImageService(DockerClient dockerClient, ILogger<DockerImageService> logger)
    {
        DockerClient = dockerClient;
        Logger = logger;
    }

    public async Task Ensure(string name, Action<string>? onProgressUpdated)
    {
        await DockerClient.Images.CreateImageAsync(new()
            {
                FromImage = name
            },
            new AuthConfig(), // TODO: Config for custom registries
            new Progress<JSONMessage>(async message =>
            {
                if (message.Progress == null)
                    return;

                var line = $"[{message.ID}] {message.ProgressMessage}";
                
                Logger.LogInformation("{line}", line);
                
                if(onProgressUpdated != null)
                    onProgressUpdated.Invoke(line);
            })
        );
    }
}