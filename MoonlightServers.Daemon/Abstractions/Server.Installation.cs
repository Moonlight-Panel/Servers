using Docker.DotNet;
using Docker.DotNet.Models;
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
        try
        {
            // TODO: Consider if checking for existing install containers is actually useful, because
            // when the daemon is starting and a installation is still ongoing it will reattach anyways
            // and the container has the auto remove flag enabled by default (maybe also consider this for the normal runtime container)

            await LogToConsole("Fetching installation configuration");

            // Fetching remote configuration and install config
            var remoteService = ServiceProvider.GetRequiredService<RemoteService>();
            
            var installData = await remoteService.GetServerInstallation(Configuration.Id);
            var serverData = await remoteService.GetServer(Configuration.Id);
            
            // We are updating the regular server config here as well
            // as changes to variables and other settings wouldn't sync otherwise
            // because they won't trigger a sync
            var serverConfiguration = serverData.ToServerConfiguration();
            UpdateConfiguration(serverConfiguration);

            var dockerImageService = ServiceProvider.GetRequiredService<DockerImageService>();

            // We call an external service for that, as we want to have a central management point of images
            // for analytics and automatic deletion
            await dockerImageService.Ensure(installData.DockerImage, async message => { await LogToConsole(message); });

            // Ensuring storage
            await EnsureInstallationVolume();
            await EnsureRuntimeVolume();

            // Write installation script to path
            var content = installData.Script.Replace("\r\n", "\n");
            await File.WriteAllTextAsync(PathBuilder.File(InstallationVolumePath, "install.sh"), content);

            // Creating container configuration
            var parameters = Configuration.ToInstallationCreateParameters(
                appConfiguration: AppConfiguration,
                RuntimeVolumePath,
                InstallationVolumePath,
                InstallationContainerName,
                installData.DockerImage,
                installData.Shell
            );

            var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

            // Ensure we can actually spawn the container

            try
            {
                var existingContainer = await dockerClient.Containers.InspectContainerAsync(InstallationContainerName);

                // Perform automatic cleanup / restore

                if (existingContainer.State.Running)
                    await dockerClient.Containers.KillContainerAsync(existingContainer.ID, new());

                await dockerClient.Containers.RemoveContainerAsync(existingContainer.ID, new());
            }
            catch (DockerContainerNotFoundException)
            {
                // Ignored
            }
            
            // Spawn the container

            var container = await dockerClient.Containers.CreateContainerAsync(parameters);
            InstallationContainerId = container.ID;

            await AttachConsole(InstallationContainerId);

            await dockerClient.Containers.StartContainerAsync(InstallationContainerId, new());
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while performing install trigger: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.NotifyInternalError);
        }
    }

    private async Task InternalFinishInstall()
    {
        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();
        
        ContainerInspectResponse? container;

        try
        {
            container = await dockerClient.Containers.InspectContainerAsync(InstallationContainerId, CancellationToken.None);
        }
        catch (DockerContainerNotFoundException)
        {
            container = null;
        }
        
        if(container == null)
            return;

        var exitCode = container.State.ExitCode;

        await LogToConsole($"Installation finished with exit code: {exitCode}");
        
        if (exitCode != 0)
        {
            // TODO: Report installation failure
        }

        await LogToConsole("Removing container");
        //await dockerClient.Containers.RemoveContainerAsync(InstallationContainerId, new());
        InstallationContainerId = null;
        
        await ResetTasks();

        await RemoveInstallationVolume();
    }
}