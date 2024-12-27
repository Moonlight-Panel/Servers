using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.DaemonShared.Enums;

namespace MoonlightServers.Daemon.Models;

public class Server
{
    public ILogger Logger { get; set; }
    public ServerConsole Console { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public ServerState State => StateMachine.CurrentState;
    public StateMachine<ServerState> StateMachine { get; set; }
    public ServerConfiguration Configuration { get; set; }
    public string? ContainerId { get; set; }
    
    // This can be used to stop streaming when the server gets destroyed or something
    public CancellationTokenSource Cancellation { get; set; }

    #region Small helpers

    public string RuntimeContainerName => $"moonlight-runtime-{Configuration.Id}";
    public string InstallContainerName => $"moonlight-install-{Configuration.Id}";

    public string RuntimeVolumePath
    {
        get
        {
            var appConfig = ServiceProvider.GetRequiredService<AppConfiguration>();
            
            var localPath = PathBuilder.Dir(
                appConfig.Storage.Volumes,
                Configuration.Id.ToString()
            );

            var absolutePath = Path.GetFullPath(localPath);

            return absolutePath;
        }
    }
    
    public string InstallVolumePath
    {
        get
        {
            var appConfig = ServiceProvider.GetRequiredService<AppConfiguration>();
            
            var localPath = PathBuilder.Dir(
                appConfig.Storage.Install,
                Configuration.Id.ToString()
            );

            var absolutePath = Path.GetFullPath(localPath);

            return absolutePath;
        }
    }

    #endregion
}