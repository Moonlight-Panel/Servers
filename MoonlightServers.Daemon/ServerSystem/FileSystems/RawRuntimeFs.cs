using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.FileSystems;

public class RawRuntimeFs : IFileSystem
{
    private readonly string BaseDirectory;

    public RawRuntimeFs(ServerContext context)
    {
        BaseDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "storage",
            "volumes",
            context.Configuration.Id.ToString()
        );
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task<string> GetPathAsync()
        => Task.FromResult(BaseDirectory);

    public Task<bool> CheckExistsAsync()
    {
        var exists = Directory.Exists(BaseDirectory);
        return Task.FromResult(exists);
    }

    public Task<bool> CheckMountedAsync()
        => Task.FromResult(true);

    public Task CreateAsync()
    {
        Directory.CreateDirectory(BaseDirectory);
        return Task.CompletedTask;
    }

    public Task PerformChecksAsync()
        => Task.CompletedTask;

    public Task MountAsync()
        => Task.CompletedTask;

    public Task UnmountAsync()
        => Task.CompletedTask;

    public Task DestroyAsync()
    {
        Directory.Delete(BaseDirectory, true);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}