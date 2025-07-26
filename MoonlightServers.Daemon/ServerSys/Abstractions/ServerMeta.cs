using MoonlightServers.Daemon.Models.Cache;

namespace MoonlightServers.Daemon.ServerSys.Abstractions;

public record ServerMeta
{
    public ServerConfiguration Configuration { get; set; }
    public IServiceCollection ServiceCollection { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
}