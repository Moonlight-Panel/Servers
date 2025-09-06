namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IReporter : IServerComponent
{
    /// <summary>
    /// Writes both in the server logs as well in the server console the provided message as a status update
    /// </summary>
    /// <param name="message">The message to write</param>
    /// <returns></returns>
    public Task StatusAsync(string message);
    
    /// <summary>
    /// Writes both in the server logs as well in the server console the provided message as an error
    /// </summary>
    /// <param name="message">The message to write</param>
    /// <returns></returns>
    public Task ErrorAsync(string message);
}