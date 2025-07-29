namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IInstaller : IServerComponent
{
    public IAsyncObservable<object> OnExited { get; }
    public bool IsRunning { get; }
    
    public Task Start();
    public Task Abort();
    public Task Cleanup();
    
    public Task<ServerCrash?> SearchForCrash();
}