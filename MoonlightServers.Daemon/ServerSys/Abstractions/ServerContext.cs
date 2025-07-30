using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public record ServerContext
{
    public ServerConfiguration Configuration { get; set; }
    public AsyncServiceScope ServiceScope { get; set; }
    public ServerInstallDataResponse InstallConfiguration { get; set; }
    public Server Self { get; set; }
}