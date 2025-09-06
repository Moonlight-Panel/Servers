namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IOnlineDetector : IServerComponent
{
    /// <summary>
    /// Creates the detection engine for the online state
    /// </summary>
    /// <returns></returns>
    public Task CreateAsync();
    
    /// <summary>
    /// Handles the detection of the online state based on the received output
    /// </summary>
    /// <param name="line">Excerpt of the output</param>
    /// <returns>True if the detection showed that the server is online. False if the detection didnt find anything</returns>
    public Task<bool> HandleOutputAsync(string line);
    
    /// <summary>
    /// Destroys the detection engine for the online state
    /// </summary>
    /// <returns></returns>
    public Task DestroyAsync();
}