namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IStatistics : IServerComponent
{
    public IAsyncObservable<ServerStats> Stats { get; }
    
    public Task SubscribeToRuntime();
    public Task SubscribeToInstallation();

    public ServerStats[] GetStats(int count);
}