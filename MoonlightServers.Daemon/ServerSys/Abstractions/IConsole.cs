namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IConsole : IServerComponent
{
    public IAsyncObservable<string> OnOutput { get; }
    public IAsyncObservable<string> OnInput { get; }
    
    public Task AttachToRuntime();
    public Task AttachToInstallation();
    
    /// <summary>
    /// Detaches any attached consoles. Usually either runtime or install is attached
    /// </summary>
    /// <returns></returns>
    public Task Detach();
    
    public Task CollectFromRuntime();
    public Task CollectFromInstallation();
    
    public Task WriteToOutput(string content);
    public Task WriteToInput(string content);
    public Task WriteToMoonlight(string content);
    
    public Task ClearOutput();
    public string[] GetOutput();
}