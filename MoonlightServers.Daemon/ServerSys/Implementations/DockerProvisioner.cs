using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Observability;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Mappers;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerProvisioner : IProvisioner
{
    public IAsyncObservable<object> OnExited => OnExitedSubject;
    public bool IsProvisioned { get; private set; }

    private readonly DockerClient DockerClient;
    private readonly ILogger<DockerProvisioner> Logger;
    private readonly DockerEventService EventService;
    private readonly ServerContext Context;
    private readonly IConsole Console;
    private readonly DockerImageService ImageService;
    private readonly ServerConfigurationMapper Mapper;
    private readonly IFileSystem FileSystem;

    private EventSubject<object> OnExitedSubject = new();

    private string? ContainerId;
    private string ContainerName;
    private IAsyncDisposable? ContainerEventSubscription;

    public DockerProvisioner(
        DockerClient dockerClient,
        ILogger<DockerProvisioner> logger,
        DockerEventService eventService,
        ServerContext context,
        IConsole console,
        DockerImageService imageService,
        ServerConfigurationMapper mapper,
        IFileSystem fileSystem
    )
    {
        DockerClient = dockerClient;
        Logger = logger;
        EventService = eventService;
        Context = context;
        Console = console;
        ImageService = imageService;
        Mapper = mapper;
        FileSystem = fileSystem;
    }

    public async Task Initialize()
    {
        ContainerName = $"moonlight-runtime-{Context.Configuration.Id}";

        ContainerEventSubscription = await EventService
            .OnContainerEvent
            .SubscribeEventAsync(HandleContainerEvent);
        
        // Check for any already existing runtime container to reclaim
        Logger.LogDebug("Searching for orphan container to reclaim");
        
        try
        {
            var container = await DockerClient.Containers.InspectContainerAsync(ContainerName);
            
            ContainerId = container.ID;
            IsProvisioned = container.State.Running;
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
    {
        return Task.CompletedTask; // TODO: Implement
    }

    public async Task Provision()
    {
        // Plan of action:
        // 1. Ensure no other container with that name exist
        // 2. Ensure the docker image has been downloaded
        // 3. Create the container from the configuration in the meta

        // 1. Ensure no other container with that name exist

        try
        {
            Logger.LogDebug("Searching for orphan container");

            var possibleContainer = await DockerClient.Containers.InspectContainerAsync(ContainerName);

            Logger.LogDebug("Orphan container found. Removing it");
            await Console.WriteToMoonlight("Found orphan container. Removing it");

            await EnsureContainerOffline(possibleContainer);

            Logger.LogDebug("Removing orphan container");
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

        // 3. Create the container from the configuration in the meta
        var hostFsPath = FileSystem.GetExternalPath();

        var parameters = Mapper.ToRuntimeParameters(
            Context.Configuration,
            hostFsPath,
            ContainerName
        );
        
        var createdContainer = await DockerClient.Containers.CreateContainerAsync(parameters);
        
        ContainerId = createdContainer.ID;
        
        Logger.LogDebug("Created container");
        await Console.WriteToMoonlight("Created container");
    }

    public async Task Start()
    {
        if(string.IsNullOrEmpty(ContainerId))
            throw new ArgumentNullException(nameof(ContainerId), "Container id of runtime is unknown");
        
        await Console.WriteToMoonlight("Starting container");
        await DockerClient.Containers.StartContainerAsync(ContainerId, new());
    }

    public async Task Stop()
    {
        if (Context.Configuration.StopCommand.StartsWith('^'))
        {
            await DockerClient.Containers.KillContainerAsync(ContainerId, new()
            {
                Signal = Context.Configuration.StopCommand.Substring(1)
            });
        }
        else
            await Console.WriteToInput(Context.Configuration.StopCommand + "\n\r");
    }

    public async Task Kill()
    {
        await EnsureContainerOffline();
    }

    public async Task Deprovision()
    {
        // Plan of action:
        // 1. Search for the container by id or name
        // 2. Ensure container is offline
        // 3. Remove the container

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
        Logger.LogDebug("Removing container");
        await Console.WriteToMoonlight("Removing container");

        await DockerClient.Containers.RemoveContainerAsync(container.ID, new());
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

    public Task<ServerCrash?> SearchForCrash()
    {
        throw new NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        OnExitedSubject.Dispose();

        if (ContainerEventSubscription != null)
            await ContainerEventSubscription.DisposeAsync();
    }
}