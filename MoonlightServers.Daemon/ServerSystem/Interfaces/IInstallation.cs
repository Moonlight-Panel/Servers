using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IInstallation : IServerComponent
{
    /// <summary>
    /// Checks if the installation environment exists. It doesn't matter if it is currently running or not
    /// </summary>
    /// <returns>True if it exists, False if it doesn't</returns>
    public Task<bool> CheckExistsAsync();

    /// <summary>
    /// Creates the installation environment
    /// </summary>
    /// <param name="runtimePath">Host path of the runtime storage location</param>
    /// <param name="hostPath">Host path of the installation file system</param>
    /// <param name="data">Installation data for the server</param>
    /// <returns></returns>
    public Task CreateAsync(string runtimePath, string hostPath, ServerInstallDataResponse data);
    
    /// <summary>
    /// Starts the installation
    /// </summary>
    /// <returns></returns>
    public Task StartAsync();
    
    /// <summary>
    /// Kills the current installation immediately
    /// </summary>
    /// <returns></returns>
    public Task KillAsync();
    
    /// <summary>
    /// Removes the installation. E.g. removes the docker container
    /// </summary>
    /// <returns></returns>
    public Task DestroyAsync();
    
    /// <summary>
    /// Subscribes to the event when the installation exists
    /// </summary>
    /// <param name="callback">Callback to invoke whenever the installation exists</param>
    /// <returns>Subscription disposable to unsubscribe from the event</returns>
    public Task<IAsyncDisposable> SubscribeExited(Func<int, ValueTask> callback);
    
    /// <summary>
    /// Connects an existing installation to this abstraction in order to restore it.
    /// E.g. fetching the container id and using it for exit events
    /// </summary>
    /// <returns></returns>
    public Task RestoreAsync();
}