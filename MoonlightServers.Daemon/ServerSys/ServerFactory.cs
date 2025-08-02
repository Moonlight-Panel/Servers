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
        
        var context = scope.ServiceProvider.GetRequiredService<ServerContext>();
        
        context.Configuration = configuration;
        context.ServiceScope = scope;
        
        var server = scope.ServiceProvider.GetRequiredService<Server>();
        
        context.Self = server;
        
        return server;
    }
}