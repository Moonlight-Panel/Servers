namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IServerComponent : IAsyncDisposable
{
    /// <summary>
    /// Initializes the server component
    /// </summary>
    /// <returns></returns>
    public Task InitializeAsync();
}