using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.ServerSys.Implementations;

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
        var serverMeta = new ServerMeta();
        serverMeta.Configuration = configuration;

        // Build service collection
        serverMeta.ServiceCollection = new ServiceCollection();
        
        // Configure service pipeline for the server components
        serverMeta.ServiceCollection.AddSingleton<IConsole, DockerConsole>();
        serverMeta.ServiceCollection.AddSingleton<IFileSystem, RawFileSystem>();
        
        // TODO: Handle implementation configurations (e.g. virtual disk) here

        // Combine both app service provider and our server instance specific one
        serverMeta.ServiceProvider = new CompositeServiceProvider([
            serverMeta.ServiceCollection.BuildServiceProvider(),
            ServiceProvider
        ]);
        
        return serverMeta.ServiceProvider.GetRequiredService<Server>();
    }
}