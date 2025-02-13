using Docker.DotNet;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Install() => await StateMachine.FireAsync(ServerTrigger.Reinstall);

    private async Task InternalInstall()
    {
        // TODO: Consider if checking for existing install containers is actually useful, because
        // when the daemon is starting and a installation is still ongoing it will reattach anyways
        // and the container has the auto remove flag enabled by default (maybe also consider this for the normal runtime container)
        
        await LogToConsole("Fetching installation configuration");
        
        // Fetching remote configuration
        var remoteService = ServiceProvider.GetRequiredService<RemoteService>();
        using var remoteHttpClient = await remoteService.CreateHttpClient();

        var installData = await remoteHttpClient.GetJson<ServerInstallDataResponse>($"api/servers/remote/servers/{Configuration.Id}/install");
        
        var dockerImageService = ServiceProvider.GetRequiredService<DockerImageService>();

        // We call an external service for that, as we want to have a central management point of images
        // for analytics and automatic deletion
        await dockerImageService.Ensure(installData.DockerImage, async message => { await LogToConsole(message); });
        
        // Ensuring storage configuration
        var installationHostPath = await EnsureInstallationVolume();
        var runtimeHostPath = await EnsureRuntimeVolume();
        
        // Write installation script to path
        await File.WriteAllTextAsync(PathBuilder.File(installationHostPath, "install.sh"), installData.Script.Replace("\n\r", "\n") + "\n\n");
        
        // Creating container configuration
        var parameters = Configuration.ToInstallationCreateParameters(
            runtimeHostPath,
            installationHostPath,
            InstallationContainerName,
            installData.DockerImage,
            installData.Shell
        );

        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        var container = await dockerClient.Containers.CreateContainerAsync(parameters);
        InstallationContainerId = container.ID;

        await AttachConsole(InstallationContainerId);

        await dockerClient.Containers.StartContainerAsync(InstallationContainerId, new());
    }
}