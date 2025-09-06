using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IStatistics : IServerComponent
{
    /// <summary>
    /// Attaches the statistics collector to the currently running runtime
    /// </summary>
    /// <returns></returns>
    public Task AttachRuntimeAsync();
    
    /// <summary>
    /// Attaches the statistics collector to the currently running installation
    /// </summary>
    /// <returns></returns>
    public Task AttachInstallationAsync();

    /// <summary>
    /// Clears the statistics cache
    /// </summary>
    /// <returns></returns>
    public Task ClearCacheAsync();
    
    /// <summary>
    /// Gets the statistics data from the cache
    /// </summary>
    /// <returns>All data from the cache</returns>
    public Task<IEnumerable<StatisticsData>> GetCacheAsync();
}