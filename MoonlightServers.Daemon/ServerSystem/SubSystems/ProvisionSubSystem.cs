using Docker.DotNet;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Services;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class ProvisionSubSystem : ServerSubSystem
{
    public string? CurrentContainerId { get; set; }

    private readonly DockerClient DockerClient;
    private readonly AppConfiguration AppConfiguration;
    private readonly RemoteService RemoteService;
    private readonly DockerImageService DockerImageService;

    public ProvisionSubSystem(
        Server server,
        ILogger logger,
        DockerClient dockerClient,
        AppConfiguration appConfiguration,
        RemoteService remoteService,
        DockerImageService dockerImageService
    ) : base(server, logger)
    {
        DockerClient = dockerClient;
        AppConfiguration = appConfiguration;
        RemoteService = remoteService;
        DockerImageService = dockerImageService;
    }

    public override Task Initialize()
    {
        StateMachine.Configure(ServerState.Starting)
            .OnEntryFromAsync(ServerTrigger.Start, HandleProvision);

        StateMachine.Configure(ServerState.Offline)
            .OnEntryAsync(HandleDeprovision);

        return Task.CompletedTask;
    }

    #region Provisioning

    private async Task HandleProvision()
    {
        try
        {
            await Provision();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while provisioning server: {e}", e);
            
            await StateMachine.FireAsync(ServerTrigger.FailSafe);
        }
    }

    private async Task Provision()
    {
        // What will happen here:
        // 1. Remove possible existing container
        // 2. Fetch latest configuration
        // 3. Ensure the storage location exists
        // 4. Ensure the docker image has been downloaded
        // 5. Create the docker container
        // 6. Attach the console
        // 7. Attach to stats
        // 8. Start the container

        // Define some shared variables:
        var containerName = $"moonlight-runtime-{Configuration.Id}";

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

        // 3. Ensure the storage location exists
        
        Logger.LogDebug("Ensuring storage");
        
        var storageSubSystem = Server.GetRequiredSubSystem<StorageSubSystem>();

        if (!await storageSubSystem.RequestRuntimeVolume())
        {
            Logger.LogDebug("Unable to continue provision because the server file system isn't ready");
            await consoleSubSystem.WriteMoonlight("Server file system is not ready yet. Try again later");
            
            await StateMachine.FireAsync(ServerTrigger.FailSafe);
            return;
        }
        
        var volumePath = storageSubSystem.RuntimeVolumePath;

        // 4. Ensure the docker image is downloaded

        Logger.LogDebug("Downloading docker image");
        await consoleSubSystem.WriteMoonlight("Downloading docker image");
        
        await DockerImageService.Download(Configuration.DockerImage, async updateMessage =>
        {
            await consoleSubSystem.WriteMoonlight(updateMessage);
        });
        
        Logger.LogDebug("Docker image downloaded");
        await consoleSubSystem.WriteMoonlight("Downloaded docker image");
        
        // 5. Create the docker container
        
        Logger.LogDebug("Creating docker container");
        await consoleSubSystem.WriteMoonlight("Creating container");

        var containerParams = Configuration.ToRuntimeCreateParameters(
            AppConfiguration,
            volumePath,
            containerName
        );

        var creationResult = await DockerClient.Containers.CreateContainerAsync(containerParams);
        CurrentContainerId = creationResult.ID;
        
        // 6. Attach the console
        
        Logger.LogDebug("Attaching console");
        await consoleSubSystem.Attach(CurrentContainerId);
        
        // 7. Attach stats stream

        var statsSubSystem = Server.GetRequiredSubSystem<StatsSubSystem>();

        await statsSubSystem.Attach(CurrentContainerId);
        
        // 8. Start the docker container
        
        Logger.LogDebug("Starting docker container");
        await consoleSubSystem.WriteMoonlight("Starting container");

        await DockerClient.Containers.StartContainerAsync(containerName, new());
    }

    #endregion

    #region Deprovision

    private async Task HandleDeprovision(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        try
        {
            await Deprovision();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while provisioning server: {e}", e);
            
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
    }

    #endregion
}