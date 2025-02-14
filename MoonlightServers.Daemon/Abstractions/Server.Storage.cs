using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    private async Task<string> EnsureRuntimeVolume()
    {
        var hostPath = GetRuntimeVolumePath();
        
        await LogToConsole("Creating storage");

        // Create volume if missing
        if (!Directory.Exists(hostPath))
            Directory.CreateDirectory(hostPath);
        
        // TODO: Virtual disk
        
        return hostPath;
    }
    
    private string GetRuntimeVolumePath()
    {
        var appConfiguration = ServiceProvider.GetRequiredService<AppConfiguration>();
        
        var hostPath = PathBuilder.Dir(
            appConfiguration.Storage.Volumes,
            Configuration.Id.ToString()
        );
        
        if (hostPath.StartsWith("/"))
            return hostPath;
        else
            return PathBuilder.JoinPaths(Directory.GetCurrentDirectory(), hostPath);
    }
    
    public async Task RemoveRuntimeVolume()
    {
        var hostPath = GetRuntimeVolumePath();
        
        await LogToConsole("Removing storage");

        // Remove volume if existing
        if (Directory.Exists(hostPath))
            Directory.Delete(hostPath, true);
        
        // TODO: Virtual disk
    }
    
    private async Task<string> EnsureInstallationVolume()
    {
        var hostPath = GetInstallationVolumePath();
        
        await LogToConsole("Creating installation storage");

        // Create volume if missing
        if (!Directory.Exists(hostPath))
            Directory.CreateDirectory(hostPath);
        
        return hostPath;
    }

    private string GetInstallationVolumePath()
    {
        var appConfiguration = ServiceProvider.GetRequiredService<AppConfiguration>();
        
        var hostPath = PathBuilder.Dir(
            appConfiguration.Storage.Install,
            Configuration.Id.ToString()
        );
        
        if (hostPath.StartsWith("/"))
            return hostPath;
        else
            return PathBuilder.JoinPaths(Directory.GetCurrentDirectory(), hostPath);
    }

    public async Task RemoveInstallationVolume()
    {
        var hostPath = GetInstallationVolumePath();
        
        await LogToConsole("Removing installation storage");

        // Remove volume if existing
        if (Directory.Exists(hostPath))
            Directory.Delete(hostPath, true);
    }
}