using MoonCore.Exceptions;
using MoonCore.Unix.SecureFs;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class StorageSubSystem : ServerSubSystem
{
    private readonly AppConfiguration AppConfiguration;
    private SecureFileSystem SecureFileSystem;
    private ServerFileSystem ServerFileSystem;
    private bool IsInitialized = false;

    public StorageSubSystem(
        Server server,
        ILogger logger,
        AppConfiguration appConfiguration
    ) : base(server, logger)
    {
        AppConfiguration = appConfiguration;
    }

    public override Task Initialize()
    {
        Logger.LogDebug("Lazy initializing server file system");
        
        Task.Run(async () =>
        {
            try
            {
                await EnsureRuntimeVolume();
                var hostPath = await GetRuntimeHostPath();

                SecureFileSystem = new(hostPath);
                ServerFileSystem = new(SecureFileSystem);
                
                IsInitialized = true;
            }
            catch (Exception e)
            {
                Logger.LogError("An unhandled error occured while lazy initializing server file system: {e}", e);
            }
        });
        
        return Task.CompletedTask;
    }

    public override async Task Delete()
    {
        await DeleteInstallVolume();
        await DeleteRuntimeVolume();
    }

    #region Runtime

    public Task<ServerFileSystem> GetFileSystem()
    {
        if (!IsInitialized)
            throw new HttpApiException("The file system is still initializing. Please try again later", 503);

        return Task.FromResult(ServerFileSystem);
    }

    public Task<bool> IsRuntimeVolumeReady()
    {
        return Task.FromResult(IsInitialized);
    }

    private async Task EnsureRuntimeVolume()
    {
        var path = await GetRuntimeHostPath();
            
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
/*
        var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

        await consoleSubSystem.WriteMoonlight("Creating virtual disk file. Please be patient");
        await Task.Delay(TimeSpan.FromSeconds(8));
        
        await consoleSubSystem.WriteMoonlight("Formatting virtual disk. This can take a bit");
        await Task.Delay(TimeSpan.FromSeconds(8));
        
        await consoleSubSystem.WriteMoonlight("Mounting virtual disk. Please be patient");
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        await consoleSubSystem.WriteMoonlight("Virtual disk ready");*/

        // TODO: Implement virtual disk 
    }
    
    public Task<string> GetRuntimeHostPath()
    {
        var path = Path.Combine(
            AppConfiguration.Storage.Volumes,
            Configuration.Id.ToString()
        );

        if (!path.StartsWith('/'))
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        
        return Task.FromResult(path);
    }

    private async Task DeleteRuntimeVolume()
    {
        var path = await GetRuntimeHostPath();
        
        if(!Directory.Exists(path))
            return;
        
        Directory.Delete(path, true);
    }

    #endregion

    #region Installation

    public async Task<string> EnsureInstallVolume()
    {
        var path = await GetInstallHostPath();
            
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }
    
    public Task<string> GetInstallHostPath()
    {
        var path = Path.Combine(
            AppConfiguration.Storage.Install,
            Configuration.Id.ToString()
        );

        if (!path.StartsWith('/'))
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        
        return Task.FromResult(path);
    }

    public async Task DeleteInstallVolume()
    {
        var path = await GetInstallHostPath();
        
        if(!Directory.Exists(path))
            return;
        
        Directory.Delete(path, true);
    }

    #endregion

    public override ValueTask DisposeAsync()
    {
        if (IsInitialized)
        {
            if(!SecureFileSystem.IsDisposed)
                SecureFileSystem.Dispose();
        }
        
        return ValueTask.CompletedTask;
    }
}