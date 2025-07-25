namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IProvisioner : IServerComponent
{
    public IAsyncObservable<object> OnExited { get; set; }
    
    public Task Start();
    public Task Stop();
    public Task Kill();
    public Task Cleanup();
    
    public Task<ServerCrash?> SearchForCrash();
}