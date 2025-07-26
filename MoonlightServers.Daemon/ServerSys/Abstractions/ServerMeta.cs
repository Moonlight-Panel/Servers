using MoonlightServers.Daemon.Models.Cache;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public record ServerContext
{
    public ServerConfiguration Configuration { get; set; }
    public AsyncServiceScope ServiceScope { get; set; }
}