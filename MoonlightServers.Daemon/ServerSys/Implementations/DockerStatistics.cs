using System.Reactive.Linq;
using System.Reactive.Subjects;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerStatistics : IStatistics
{
    public IAsyncObservable<ServerStats> OnStats => OnStatsSubject.ToAsyncObservable();

    private readonly Subject<ServerStats> OnStatsSubject = new();
    
    public Task Initialize()
        => Task.CompletedTask;

    public Task Sync()
        => Task.CompletedTask;
    
    public Task SubscribeToRuntime()
     => Task.CompletedTask;

    public Task SubscribeToInstallation()
    => Task.CompletedTask;

    public ServerStats[] GetStats(int count)
    {
        return [];
    }
    
    public async ValueTask DisposeAsync()
    {
        OnStatsSubject.Dispose();
    }
}