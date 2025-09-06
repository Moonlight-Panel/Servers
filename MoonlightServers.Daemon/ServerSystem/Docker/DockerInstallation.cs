using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Events;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using MoonlightServers.Daemon.Services;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.ServerSystem.Docker;

public class DockerInstallation : IInstallation
{
    private readonly DockerEventService DockerEventService;
    private readonly ServerConfigurationMapper Mapper;
    private readonly DockerImageService ImageService;
    private readonly ServerContext ServerContext;
    private readonly DockerClient DockerClient;
    private readonly IReporter Reporter;

    private readonly EventSource<int> ExitEventSource = new();

    private IAsyncDisposable ContainerEventSubscription;
    private string ContainerId;

    public DockerInstallation(
        DockerClient dockerClient,
        ServerContext serverContext,
        ServerConfigurationMapper mapper,
        DockerImageService imageService,
        IReporter reporter,
        DockerEventService dockerEventService
    )
    {
        DockerClient = dockerClient;
        ServerContext = serverContext;
        Mapper = mapper;
        ImageService = imageService;
        Reporter = reporter;
        DockerEventService = dockerEventService;
    }

    public async Task InitializeAsync()
    {
        ContainerEventSubscription = await DockerEventService.SubscribeContainerAsync(OnContainerEvent);
    }

    private async ValueTask OnContainerEvent(Message message)
    {
        // Only handle events for our own container
        if (message.ID != ContainerId)
            return;

        // Only handle die events
        if (message.Action != "die")
            return;

        int exitCode;

        if (message.Actor.Attributes.TryGetValue("exitCode", out var exitCodeStr))
        {
            if (!int.TryParse(exitCodeStr, out exitCode))
                exitCode = 0;
        }
        else
            exitCode = 0;


        await ExitEventSource.InvokeAsync(exitCode);
    }

    public async Task<bool> CheckExistsAsync()
    {
        try
        {
            var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

            await DockerClient.Containers.InspectContainerAsync(
                containerName
            );

            return true;
        }
        catch (DockerContainerNotFoundException)
        {
            return false;
        }
    }

    public async Task CreateAsync(
        string runtimePath,
        string hostPath,
        ServerInstallDataResponse data
    )
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

        var parameters = Mapper.ToInstallParameters(
            ServerContext.Configuration,
            data,
            runtimePath,
            hostPath,
            containerName
        );

        // Docker image
        await Reporter.StatusAsync("Downloading docker image");

        await ImageService.Download(data.DockerImage, async status => { await Reporter.StatusAsync(status); });

        await Reporter.StatusAsync("Downloaded docker image");

        // Write install script to install fs

        await File.WriteAllTextAsync(
            Path.Combine(hostPath, "install.sh"),
            data.Script
        );

        //

        await DockerClient.Containers.CreateContainerAsync(parameters);

        await Reporter.StatusAsync("Created container");
    }

    public async Task StartAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

        await DockerClient.Containers.StartContainerAsync(containerName, new());
    }

    public async Task KillAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

        await DockerClient.Containers.KillContainerAsync(containerName, new());
    }

    public async Task DestroyAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

        try
        {
            var container = await DockerClient.Containers.InspectContainerAsync(containerName);

            if (container.State.Running)
                await DockerClient.Containers.KillContainerAsync(containerName, new());

            await DockerClient.Containers.RemoveContainerAsync(containerName, new()
            {
                Force = true
            });
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }
    }

    public async Task<IAsyncDisposable> SubscribeExited(Func<int, ValueTask> callback)
        => await ExitEventSource.SubscribeAsync(callback);

    public async Task RestoreAsync()
    {
        try
        {
            var containerName = string.Format(DockerConstants.InstallationNameTemplate, ServerContext.Configuration.Id);

            var container = await DockerClient.Containers.InspectContainerAsync(containerName);
            ContainerId = container.ID;
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignore
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ContainerEventSubscription.DisposeAsync();
    }
}