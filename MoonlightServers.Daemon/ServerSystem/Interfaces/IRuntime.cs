namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IRuntime : IServerComponent
{
    /// <summary>
    /// Checks if the runtime does exist. This includes already running instances
    /// </summary>
    /// <returns>True if it exists, False if it doesn't</returns>
    public Task<bool> CheckExistsAsync();

    /// <summary>
    /// Creates the runtime with the specified path as the storage path where the server files should be stored in
    /// </summary>
    /// <param name="path">Path where the server files are located</param>
    /// <returns></returns>
    public Task CreateAsync(string path);
    
    /// <summary>
    /// Starts the runtime. This requires <see cref="CreateAsync"/> to be called before this function
    /// </summary>
    /// <returns></returns>
    public Task StartAsync();
    
    /// <summary>
    /// Performs a live update on the runtime. When this method is called the current server configuration has already been updated
    /// </summary>
    /// <returns></returns>
    public Task UpdateAsync();
    
    /// <summary>
    /// Kills the current runtime immediately
    /// </summary>
    /// <returns></returns>
    public Task KillAsync();
    
    /// <summary>
    /// Destroys the runtime. When implemented using docker this would remove the container used for hosting the runtime
    /// </summary>
    /// <returns></returns>
    public Task DestroyAsync();

    /// <summary>
    /// This subscribes to the exited event of the runtime
    /// </summary>
    /// <param name="callback">Callback gets invoked whenever the runtime exites</param>
    /// <returns>Subscription disposable to unsubscribe from the event</returns>
    public Task<IAsyncDisposable> SubscribeExited(Func<int, Task> callback);

    /// <summary>
    /// Connects an existing runtime to this abstraction in order to restore it.
    /// E.g. fetching the container id and using it for exit events
    /// </summary>
    /// <returns></returns>
    public Task RestoreAsync();
}