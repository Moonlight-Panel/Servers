using Docker.DotNet;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class InstallationSubSystem : ServerSubSystem
{
    public string? CurrentContainerId { get; set; }

    private readonly DockerClient DockerClient;
    private readonly RemoteService RemoteService;
    private readonly DockerImageService DockerImageService;
    private readonly AppConfiguration AppConfiguration;

    public InstallationSubSystem(
        Server server,
        ILogger logger,
        DockerClient dockerClient,
        RemoteService remoteService,
        DockerImageService dockerImageService,
        AppConfiguration appConfiguration
    ) : base(server, logger)
    {
        DockerClient = dockerClient;
        RemoteService = remoteService;
        DockerImageService = dockerImageService;
        AppConfiguration = appConfiguration;
    }

    public override Task Initialize()
    {
        StateMachine.Configure(ServerState.Installing)
            .OnEntryAsync(HandleProvision);

        StateMachine.Configure(ServerState.Installing)
            .OnExitAsync(HandleDeprovision);
        
        return Task.CompletedTask;
    }

    #region Provision

    private async Task HandleProvision()
    {
        try
        {
            await Provision();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while provisioning installation: {e}", e);

            await StateMachine.FireAsync(ServerTrigger.FailSafe);
        }
    }

    private async Task Provision()
    {
        // What will happen here:
        // 1. Remove possible existing container
        // 2. Fetch latest configuration & install configuration
        // 3. Ensure the storage location exists
        // 4. Copy script to set location
        // 5. Ensure the docker image has been downloaded
        // 6. Create the docker container
        // 7. Attach the console
        // 8. Start the container

        // Define some shared variables:
        var containerName = $"moonlight-install-{Configuration.Id}";

        var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

        // Reset container tracking id, so if we kill an old container it won't
        // trigger an Exited event :>
        CurrentContainerId = null;

        // 1. Remove possible existing container

        try
        {
            var existingContainer = await DockerClient.Containers
                .InspectContainerAsync(containerName);

            if (existingContainer.State.Running)
            {
                Logger.LogDebug("Killing old docker container");
                await consoleSubSystem.WriteMoonlight("Killing old container");

                await DockerClient.Containers.KillContainerAsync(existingContainer.ID, new());
            }

            Logger.LogDebug("Removing old docker container");
            await consoleSubSystem.WriteMoonlight("Removing old container");

            await DockerClient.Containers.RemoveContainerAsync(existingContainer.ID, new());
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }

        // 2. Fetch latest configuration

        Logger.LogDebug("Fetching latest configuration from panel");
        await consoleSubSystem.WriteMoonlight("Updating configuration");

        var serverData = await RemoteService.GetServer(Configuration.Id);
        var latestConfiguration = serverData.ToServerConfiguration();

        Server.Configuration = latestConfiguration;

        var installData = await RemoteService.GetServerInstallation(Configuration.Id);

        // 3. Ensure the storage location exists

        Logger.LogDebug("Ensuring storage");

        var storageSubSystem = Server.GetRequiredSubSystem<StorageSubSystem>();
        
        if (!await storageSubSystem.IsRuntimeVolumeReady())
        {
            Logger.LogDebug("Unable to continue provision because the server file system isn't ready");
            await consoleSubSystem.WriteMoonlight("Server file system is not ready yet. Try again later");
            
            await StateMachine.FireAsync(ServerTrigger.FailSafe);
            return;
        }
        
        var runtimePath = await storageSubSystem.GetRuntimeHostPath();
        
        var installPath = await storageSubSystem.EnsureInstallVolume();
        
        // 4. Copy script to location
        
        var content = installData.Script.Replace("\r\n", "\n");
        await File.WriteAllTextAsync(Path.Combine(installPath, "install.sh"), content);

        // 5. Ensure the docker image is downloaded

        Logger.LogDebug("Downloading docker image");
        await consoleSubSystem.WriteMoonlight("Downloading docker image");

        await DockerImageService.Download(installData.DockerImage,
            async updateMessage => { await consoleSubSystem.WriteMoonlight(updateMessage); });

        Logger.LogDebug("Docker image downloaded");
        await consoleSubSystem.WriteMoonlight("Downloaded docker image");

        // 6. Create the docker container

        Logger.LogDebug("Creating docker container");
        await consoleSubSystem.WriteMoonlight("Creating container");

        var containerParams = Configuration.ToInstallationCreateParameters(
            AppConfiguration,
            runtimePath,
            installPath,
            containerName,
            installData.DockerImage,
            installData.Shell
        );

        var creationResult = await DockerClient.Containers.CreateContainerAsync(containerParams);
        CurrentContainerId = creationResult.ID;

        // 7. Attach the console

        Logger.LogDebug("Attaching console");
        await consoleSubSystem.Attach(CurrentContainerId);

        // 8. Start the docker container

        Logger.LogDebug("Starting docker container");
        await consoleSubSystem.WriteMoonlight("Starting container");

        await DockerClient.Containers.StartContainerAsync(containerName, new());
    }

    #endregion

    #region Deprovision

    private async Task HandleDeprovision()
    {
        try
        {
            await Deprovision();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while deprovisioning installation: {e}", e);

            await StateMachine.FireAsync(ServerTrigger.FailSafe);
        }
    }

    private async Task Deprovision()
    {
        // Handle possible unknown container id calls
        if (string.IsNullOrEmpty(CurrentContainerId))
        {
            Logger.LogDebug("Skipping deprovisioning as the current container id is not set");
            return;
        }
        
        var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();
            
        // Destroy container
        
        try
        {
            Logger.LogDebug("Removing docker container");
            await consoleSubSystem.WriteMoonlight("Removing container");
            
            await DockerClient.Containers.RemoveContainerAsync(CurrentContainerId, new());
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }

        CurrentContainerId = null;
        
        // Remove install volume

        var storageSubSystem = Server.GetRequiredSubSystem<StorageSubSystem>();

        Logger.LogDebug("Removing installation data");
        await consoleSubSystem.WriteMoonlight("Removing installation data");
        
        await storageSubSystem.DeleteInstallVolume();
    }

    #endregion
}