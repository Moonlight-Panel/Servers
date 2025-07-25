namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IServerComponent : IAsyncDisposable
{
    public Task Initialize();
    public Task Sync();
}