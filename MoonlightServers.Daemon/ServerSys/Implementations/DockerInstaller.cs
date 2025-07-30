using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Observability;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerInstaller : IInstaller
{
    public IAsyncObservable<object> OnExited => OnExitedSubject;
    public bool IsRunning { get; private set; } = false;

    private readonly EventSubject<Message> OnExitedSubject = new();

    private readonly ILogger<DockerInstaller> Logger;
    private readonly DockerEventService EventService;
    private readonly IConsole Console;
    private readonly DockerClient DockerClient;
    private readonly ServerContext Context;
    private readonly DockerImageService ImageService;
    private readonly IFileSystem FileSystem;
    private readonly AppConfiguration Configuration;
    private readonly ServerConfigurationMapper Mapper;

    private string? ContainerId;
    private string ContainerName;

    private string InstallHostPath;
    
    private IAsyncDisposable? ContainerEventSubscription;

    public DockerInstaller(
        ILogger<DockerInstaller> logger,
        DockerEventService eventService,
        IConsole console,
        DockerClient dockerClient,
        ServerContext context,
        DockerImageService imageService,
        IFileSystem fileSystem,
        AppConfiguration configuration,
        ServerConfigurationMapper mapper
    )
    {
        Logger = logger;
        EventService = eventService;
        Console = console;
        DockerClient = dockerClient;
        Context = context;
        ImageService = imageService;
        FileSystem = fileSystem;
        Configuration = configuration;
        Mapper = mapper;
    }

    public async Task Initialize()
    {
        ContainerName = $"moonlight-install-{Context.Configuration.Id}";
        InstallHostPath =
            Path.GetFullPath(Path.Combine(Configuration.Storage.Install, Context.Configuration.Id.ToString()));
        
        ContainerEventSubscription = await EventService
            .OnContainerEvent
            .SubscribeEventAsync(HandleContainerEvent);
        
        // Check for any already existing runtime container to reclaim
        Logger.LogDebug("Searching for orphan container to reclaim");
        
        try
        {
            var container = await DockerClient.Containers.InspectContainerAsync(ContainerName);
            
            ContainerId = container.ID;
            IsRunning = container.State.Running;
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }
    }
    
    private async ValueTask HandleContainerEvent(Message message)
    {
        // Only handle events for our own container
        if (message.ID != ContainerId)
            return;

        // Only handle die events
        if (message.Action != "die")
            return;

        await OnExitedSubject.OnNextAsync(message);
    }

    public Task Sync()
        => Task.CompletedTask;

    public async Task Setup()
    {
        // Plan of action:
        // 1. Ensure no other container with that name exist
        // 2. Ensure the docker image has been downloaded
        // 3. Create the installation volume and place script in there  
        // 4. Create the container from the configuration in the meta
        
         // 1. Ensure no other container with that name exist
        try
        {
            Logger.LogDebug("Searching for orphan container");

            var possibleContainer = await DockerClient.Containers.InspectContainerAsync(ContainerName);

            Logger.LogDebug("Orphan container found. Removing it");
            await Console.WriteToMoonlight("Found orphan container. Removing it");

            await EnsureContainerOffline(possibleContainer);

            Logger.LogInformation("Removing orphan container");
            await DockerClient.Containers.RemoveContainerAsync(ContainerName, new());
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored
        }

        // 2. Ensure the docker image has been downloaded
        await Console.WriteToMoonlight("Downloading docker image");

        await ImageService.Download(Context.Configuration.DockerImage, async message =>
        {
            try
            {
                await Console.WriteToMoonlight(message);
            }
            catch (Exception)
            {
                // Ignored. Not handling it here could cause an application wide crash afaik
            }
        });

        // 3. Create the installation volume and place script in there
        
        await Console.WriteToMoonlight("Creating storage");
        
        if(Directory.Exists(InstallHostPath))
            Directory.Delete(InstallHostPath, true);

        Directory.CreateDirectory(InstallHostPath);

        await File.WriteAllTextAsync(Path.Combine(InstallHostPath, "install.sh"), Context.InstallConfiguration.Script);

        // 4. Create the container from the configuration in the meta
        var runtimeFsPath = FileSystem.GetExternalPath();

        var parameters = Mapper.ToInstallParameters(
            Context.Configuration,
            Context.InstallConfiguration,
            runtimeFsPath,
            InstallHostPath,
            ContainerName
        );

        var createdContainer = await DockerClient.Containers.CreateContainerAsync(parameters);

        ContainerId = createdContainer.ID;

        Logger.LogDebug("Created container");
        await Console.WriteToMoonlight("Created container");
    }

    public async Task Start()
    {
        Logger.LogDebug("Starting container");
        await Console.WriteToMoonlight("Starting container");
        await DockerClient.Containers.StartContainerAsync(ContainerId, new());
    }

    public async Task Abort()
    {
        await EnsureContainerOffline();
    }

    public async Task Cleanup()
    {
        // Plan of action:
        // 1. Search for the container by id or name
        // 2. Ensure container is offline
        // 3. Remove the container
        // 4. Delete installation volume if it exists
        
        // 1. Search for the container by id or name
        ContainerInspectResponse? container = null;

        try
        {
            if (string.IsNullOrEmpty(ContainerId))
                container = await DockerClient.Containers.InspectContainerAsync(ContainerName);
            else
                container = await DockerClient.Containers.InspectContainerAsync(ContainerId);
        }
        catch (DockerContainerNotFoundException)
        {
            // Ignored

            Logger.LogDebug("Runtime container could not be found. Reporting deprovision success");
        }

        // No container found? We are done here then
        if (container == null)
            return;

        // 2. Ensure container is offline
        await EnsureContainerOffline(container);

        // 3. Remove the container
        Logger.LogInformation("Removing container");
        await Console.WriteToMoonlight("Removing container");

        await DockerClient.Containers.RemoveContainerAsync(container.ID, new());
        
        // 4. Delete installation volume if it exists

        if (Directory.Exists(InstallHostPath))
        {
            Logger.LogInformation("Removing storage");
            await Console.WriteToMoonlight("Removing storage");
            
            Directory.Delete(InstallHostPath, true);
        }
    }

    public async Task<ServerCrash?> SearchForCrash()
    {
        return null;
    }

    private async Task EnsureContainerOffline(ContainerInspectResponse? container = null)
    {
        try
        {
            if (string.IsNullOrEmpty(ContainerId))
                container = await DockerClient.Containers.InspectContainerAsync(ContainerName);
            else
                container = await DockerClient.Containers.InspectContainerAsync(ContainerId);
        }
        catch (DockerContainerNotFoundException)
        {
            Logger.LogDebug("No container found to ensure its offline");
            
            // Ignored
        }

        // No container found? We are done here then
        if (container == null)
            return;

        // Check if container is running
        if (!container.State.Running)
            return;

        await Console.WriteToMoonlight("Killing container");
        await DockerClient.Containers.KillContainerAsync(ContainerId, new());
    }

    public async ValueTask DisposeAsync()
    {
        OnExitedSubject.Dispose();
        
        if (ContainerEventSubscription != null)
            await ContainerEventSubscription.DisposeAsync();
    }
}