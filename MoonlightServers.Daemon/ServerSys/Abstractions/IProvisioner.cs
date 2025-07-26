namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IProvisioner : IServerComponent
{
    public IObservable<object> OnExited { get; }
    public bool IsProvisioned { get; }

    public Task Provision();
    public Task Start();
    public Task Stop();
    public Task Kill();
    public Task Deprovision();
    
    public Task<ServerCrash?> SearchForCrash();
}