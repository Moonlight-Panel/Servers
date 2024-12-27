using Docker.DotNet;
using Docker.DotNet.Models;
using MoonlightServers.Daemon.Models;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerImageExtensions
{
    public static async Task EnsureDockerImage(this Server server)
    {
        await server.NotifyTask(ServerTask.PullingDockerImage);

        var dockerClient = server.ServiceProvider.GetRequiredService<DockerClient>();

        await dockerClient.Images.CreateImageAsync(new()
            {
                FromImage = server.Configuration.DockerImage
            },
            new AuthConfig(),
            new Progress<JSONMessage>(async message =>
            {
                if (message.Progress == null)
                    return;

                var percentage = message.Progress.Total > 0
                    ? Math.Round((float)message.Progress.Current / message.Progress.Total * 100f, 2)
                    : 0d;

                server.Logger.LogInformation(
                    "Docker Image: [{id}] {status} - {percent}",
                    message.ID,
                    message.Status,
                    percentage
                );

                //await UpdateProgress(server, serviceProvider, percentage);
            })
        );
    }
}