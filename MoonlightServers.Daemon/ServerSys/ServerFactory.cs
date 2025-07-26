using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys;

public class ServerFactory
{
    private readonly IServiceProvider ServiceProvider;

    public ServerFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public Server CreateServer(ServerConfiguration configuration)
    {
        var scope = ServiceProvider.CreateAsyncScope();
        
        var meta = scope.ServiceProvider.GetRequiredService<ServerContext>();
        
        meta.Configuration = configuration;
        meta.ServiceScope = scope;
        
        return scope.ServiceProvider.GetRequiredService<Server>();
    }
}