using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Docker;

public class DockerStatistics : IStatistics
{
    public Task InitializeAsync()
    => Task.CompletedTask;

    public Task AttachRuntimeAsync()
    => Task.CompletedTask;

    public Task AttachInstallationAsync()
    => Task.CompletedTask;

    public Task ClearCacheAsync()
    => Task.CompletedTask;

    public Task<IEnumerable<StatisticsData>> GetCacheAsync()
        => Task.FromResult<IEnumerable<StatisticsData>>([]);
    
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}