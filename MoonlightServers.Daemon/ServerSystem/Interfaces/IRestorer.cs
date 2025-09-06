namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IRestorer : IServerComponent
{
    /// <summary>
    /// Checks for any running runtime environment from which the state can be restored from
    /// </summary>
    /// <returns></returns>
    public Task<bool> HandleRuntimeAsync();
    
    /// <summary>
    /// Checks for any running installation environment from which the state can be restored from
    /// </summary>
    /// <returns></returns>
    public Task<bool> HandleInstallationAsync();
}