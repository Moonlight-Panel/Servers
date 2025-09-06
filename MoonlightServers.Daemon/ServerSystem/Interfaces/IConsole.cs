namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IConsole : IServerComponent
{
    /// <summary>
    /// Writes to the standard input of the console. If attached to the runtime when using docker for example this
    /// would write into the containers standard input.
    /// <remarks>This method does not add a newline separator at the end of the content. The caller needs to add this themselves if required</remarks>
    /// </summary>
    /// <param name="content">Content to write</param>
    /// <returns></returns>
    public Task WriteStdInAsync(string content);
    /// <summary>
    /// Writes to the standard output of the console. If attached to the runtime when using docker for example this
    /// would write into the containers standard output.
    /// <remarks>This method does not add a newline separator at the end of the content. The caller needs to add this themselves if required</remarks>
    /// </summary>
    /// <param name="content">Content to write</param>
    /// <returns></returns>
    public Task WriteStdOutAsync(string content);

    /// <summary>
    /// Attaches the console to the runtime environment
    /// </summary>
    /// <returns></returns>
    public Task AttachRuntimeAsync();
    
    /// <summary>
    /// Attaches the console to the installation environment
    /// </summary>
    /// <returns></returns>
    public Task AttachInstallationAsync();

    /// <summary>
    /// Fetches all output from the runtime environment and write them into the cache without triggering any events 
    /// </summary>
    /// <returns></returns>
    public Task FetchRuntimeAsync();
    
    /// <summary>
    /// Fetches all output from the installation environment and write them into the cache without triggering any events
    /// </summary>
    /// <returns></returns>
    public Task FetchInstallationAsync();
    
    /// <summary>
    /// Clears the cache of the standard output received by the environments
    /// </summary>
    /// <returns></returns>
    public Task ClearCacheAsync();
    
    /// <summary>
    /// Gets the content from the standard output cache
    /// </summary>
    /// <returns>Content from the cache</returns>
    public Task<IEnumerable<string>> GetCacheAsync();

    /// <summary>
    /// Subscribes to standard output receive events
    /// </summary>
    /// <param name="callback">Callback which will be invoked whenever a new line is received</param>
    /// <returns>Subscription disposable to unsubscribe from the event</returns>
    public Task<IAsyncDisposable> SubscribeStdOutAsync(Func<string, ValueTask> callback);
}