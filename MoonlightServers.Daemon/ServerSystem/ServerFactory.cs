using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem;

public class ServerFactory
{
    private readonly IServiceProvider ServiceProvider;

    public ServerFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public async Task<Server> Create(ServerConfiguration configuration)
    {
        var scope = ServiceProvider.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger($"Servers.Instance.{configuration.Id}.{nameof(Server)}");

        var context = scope.ServiceProvider.GetRequiredService<ServerContext>();

        context.Identifier = configuration.Id;
        context.Configuration = configuration;
        context.ServiceScope = scope;

        // Define all required components
        
        IConsole console;
        IFileSystem runtimeFs;
        IFileSystem installFs;
        IInstallation installation;
        IOnlineDetector onlineDetector;
        IReporter reporter;
        IRestorer restorer;
        IRuntime runtime;
        IStatistics statistics;
        
        // Resolve the components
        // TODO: Add a plugin hook for dynamically resolving components and checking if any is unset
        
        // Resolve server from di
        var server = new Server(
            logger,
            context,
            // Now all components
            console,
            runtimeFs,
            installFs,
            installation,
            onlineDetector,
            reporter,
            restorer,
            runtime,
            statistics,
            // And now all the handlers
            []
        );

        context.Server = server;

        return server;
    }
}