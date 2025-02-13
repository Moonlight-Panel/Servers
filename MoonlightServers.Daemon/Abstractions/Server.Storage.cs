using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    private async Task<string> EnsureRuntimeVolume()
    {
        var appConfiguration = ServiceProvider.GetRequiredService<AppConfiguration>();

        var hostPath = PathBuilder.Dir(
            appConfiguration.Storage.Volumes,
            Configuration.Id.ToString()
        );
        
        await LogToConsole("Creating storage");

        // TODO: Virtual disk
        
        // Create volume if missing
        if (!Directory.Exists(hostPath))
            Directory.CreateDirectory(hostPath);

        if (hostPath.StartsWith("/"))
            return hostPath;
        else
            return PathBuilder.JoinPaths(Directory.GetCurrentDirectory(), hostPath);
        
        return hostPath;
    }
    
    private async Task<string> EnsureInstallationVolume()
    {
        var appConfiguration = ServiceProvider.GetRequiredService<AppConfiguration>();

        var hostPath = PathBuilder.Dir(
            appConfiguration.Storage.Install,
            Configuration.Id.ToString()
        );
        
        await LogToConsole("Creating installation storage");

        // Create volume if missing
        if (!Directory.Exists(hostPath))
            Directory.CreateDirectory(hostPath);

        if (hostPath.StartsWith("/"))
            return hostPath;
        else
            return PathBuilder.JoinPaths(Directory.GetCurrentDirectory(), hostPath);
        
        return hostPath;
    }
}