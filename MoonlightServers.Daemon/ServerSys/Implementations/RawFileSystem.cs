using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class RawFileSystem : IFileSystem
{
    public bool IsMounted { get; private set; }
    public bool Exists { get; private set; }

    private readonly ServerMeta Meta;
    private readonly AppConfiguration Configuration;
    private string HostPath => Path.Combine(Configuration.Storage.Volumes, Meta.Configuration.Id.ToString());

    public RawFileSystem(ServerMeta meta, AppConfiguration configuration)
    {
        Meta = meta;
        Configuration = configuration;
    }

    public Task Initialize()
        => Task.CompletedTask;

    public Task Sync()
        => Task.CompletedTask;

    public Task Create()
    {
        return Task.CompletedTask;
    }

    public Task Mount()
    {
        IsMounted = true;
        return Task.CompletedTask;
    }

    public Task Unmount()
    {
        IsMounted = false;
        return Task.CompletedTask;
    }

    public Task Delete()
    {
        Directory.Delete(HostPath, true);
        return Task.CompletedTask;
    }

    public string GetExternalPath()
     => HostPath;

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}