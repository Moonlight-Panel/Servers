using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class RawFileSystem : IFileSystem
{
    public bool IsMounted { get; private set; }
    public bool Exists { get; private set; }

    private readonly ServerContext Context;
    private readonly AppConfiguration Configuration;
    private string HostPath;

    public RawFileSystem(ServerContext context, AppConfiguration configuration)
    {
        Context = context;
        Configuration = configuration;
    }

    public Task Initialize()
    {
        HostPath = Path.Combine(Directory.GetCurrentDirectory(), Configuration.Storage.Volumes, Context.Configuration.Id.ToString());
        
        return Task.CompletedTask;
    }

    public Task Sync()
        => Task.CompletedTask;

    public Task Create()
    {
        Directory.CreateDirectory(HostPath);
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