namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IConsole : IServerComponent
{
    public IAsyncObservable<string> OnOutput { get; }
    public IAsyncObservable<string> OnInput { get; }
    
    public Task AttachToRuntime();
    public Task AttachToInstallation();
    
    public Task WriteToOutput(string content);
    public Task WriteToInput(string content);
    public Task WriteToMoonlight(string content);
    
    public Task ClearOutput();
    public string[] GetOutput();
}