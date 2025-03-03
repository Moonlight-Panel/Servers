using MoonCore.Helpers;
using MoonCore.Unix.SecureFs;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public ServerFileSystem FileSystem { get; private set; }
    
    private SpinLock FsLock = new();
    
    private SecureFileSystem? InternalFileSystem;
    
    private string RuntimeVolumePath;
    private string InstallationVolumePath;

    private async Task InitializeStorage()
    {
        #region Configure paths

        var appConfiguration = ServiceProvider.GetRequiredService<AppConfiguration>();

        // Runtime
        var runtimePath = PathBuilder.Dir(appConfiguration.Storage.Volumes, Configuration.Id.ToString());

        if (appConfiguration.Storage.Volumes.StartsWith("/"))
            RuntimeVolumePath = runtimePath;
        else
            RuntimeVolumePath = PathBuilder.Dir(Directory.GetCurrentDirectory(), runtimePath);
        
        // Installation
        var installationPath = PathBuilder.Dir(appConfiguration.Storage.Install, Configuration.Id.ToString());

        if (appConfiguration.Storage.Install.StartsWith("/"))
            InstallationVolumePath = installationPath;
        else
            InstallationVolumePath = PathBuilder.Dir(Directory.GetCurrentDirectory(), installationPath);

        #endregion

        await ConnectRuntimeVolume();
    }

    public async Task DestroyStorage()
    {
        await DisconnectRuntimeVolume();
    }
    
    private async Task ConnectRuntimeVolume()
    {
        var gotLock = false;
        
        try
        {
            FsLock.Enter(ref gotLock);
            
            // We want to dispose the old fs if existing, to make sure we wont leave any file descriptors open
            if(InternalFileSystem != null && !InternalFileSystem.IsDisposed)
                InternalFileSystem.Dispose();

            await EnsureRuntimeVolume();

            InternalFileSystem = new SecureFileSystem(RuntimeVolumePath);
            FileSystem = new ServerFileSystem(InternalFileSystem);
        }
        finally
        {
            if(gotLock)
                FsLock.Exit();
        }
    }

    private Task DisconnectRuntimeVolume()
    {
        if(InternalFileSystem != null && !InternalFileSystem.IsDisposed)
            InternalFileSystem.Dispose();
        
        return Task.CompletedTask;
    }
    
    private Task EnsureRuntimeVolume()
    {
        if (!Directory.Exists(RuntimeVolumePath))
            Directory.CreateDirectory(RuntimeVolumePath);
        
        // TODO: Virtual disk
        
        return Task.CompletedTask;
    }
    
    public Task RemoveRuntimeVolume()
    {
        // Remove volume if existing
        if (Directory.Exists(RuntimeVolumePath))
            Directory.Delete(RuntimeVolumePath, true);
        
        // TODO: Virtual disk
        
        return Task.CompletedTask;
    }
    
    private Task EnsureInstallationVolume()
    {
        // Create volume if missing
        if (!Directory.Exists(InstallationVolumePath))
            Directory.CreateDirectory(InstallationVolumePath);
        
        return Task.CompletedTask;
    }

    public Task RemoveInstallationVolume()
    {
        // Remove install volume if existing
        if (Directory.Exists(InstallationVolumePath))
            Directory.Delete(InstallationVolumePath, true);
        
        return Task.CompletedTask;
    }
}