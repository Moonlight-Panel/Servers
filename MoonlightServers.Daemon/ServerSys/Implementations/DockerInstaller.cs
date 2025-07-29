using System.Reactive.Linq;
using System.Reactive.Subjects;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerInstaller : IInstaller
{
    public IAsyncObservable<object> OnExited => OnExitedSubject.ToAsyncObservable();
    public bool IsRunning { get; private set; } = false;

    private readonly Subject<string> OnExitedSubject = new();

    private readonly ILogger<DockerInstaller> Logger;
    private readonly DockerEventService EventService;

    private string? ContainerId;
    private string? ContainerName;

    public DockerInstaller(
        ILogger<DockerInstaller> logger,
        DockerEventService eventService
    )
    {
        Logger = logger;
        EventService = eventService;
    }

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    public Task Sync()
    {
        throw new NotImplementedException();
    }

    public Task Start()
    {
        throw new NotImplementedException();
    }

    public Task Abort()
    {
        throw new NotImplementedException();
    }

    public Task Cleanup()
    {
        throw new NotImplementedException();
    }

    public Task<ServerCrash?> SearchForCrash()
    {
        throw new NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        OnExitedSubject.Dispose();
    }
}