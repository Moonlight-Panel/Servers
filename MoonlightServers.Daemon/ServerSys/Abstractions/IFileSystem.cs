namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public interface IFileSystem : IServerComponent
{
    public bool IsMounted { get; }
    public bool Exists { get; }

    public Task Create();
    public Task Mount();
    public Task Unmount();
    public Task Delete();
    
    public string GetExternalPath();
}