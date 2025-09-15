using MoonlightServers.Daemon.Models.Cache;
using MoonlightServers.Daemon.ServerSystem.Docker;
using MoonlightServers.Daemon.ServerSystem.FileSystems;
using MoonlightServers.Daemon.ServerSystem.Handlers;
using MoonlightServers.Daemon.ServerSystem.Implementations;
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

    public async Task<Server> CreateAsync(ServerConfiguration configuration)
    {
        var scope = ServiceProvider.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger($"Servers.Instance.{configuration.Id}.{nameof(Server)}");

        var context = scope.ServiceProvider.GetRequiredService<ServerContext>();

        context.Identifier = configuration.Id;
        context.Configuration = configuration;
        context.ServiceScope = scope;
        context.Logger = logger;

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

        console = ActivatorUtilities.CreateInstance<DockerConsole>(scope.ServiceProvider);
        reporter = ActivatorUtilities.CreateInstance<ServerReporter>(scope.ServiceProvider);
        runtimeFs = ActivatorUtilities.CreateInstance<RawRuntimeFs>(scope.ServiceProvider);
        installFs = ActivatorUtilities.CreateInstance<RawInstallationFs>(scope.ServiceProvider);
        installation = ActivatorUtilities.CreateInstance<DockerInstallation>(scope.ServiceProvider);
        onlineDetector = ActivatorUtilities.CreateInstance<RegexOnlineDetector>(scope.ServiceProvider);
        restorer = ActivatorUtilities.CreateInstance<DockerRestorer>(scope.ServiceProvider);
        runtime = ActivatorUtilities.CreateInstance<DockerRuntime>(scope.ServiceProvider);
        statistics = ActivatorUtilities.CreateInstance<DockerStatistics>(scope.ServiceProvider);
        
        // Resolve handlers
        var handlers = new List<IServerStateHandler>();
        
        handlers.Add(ActivatorUtilities.CreateInstance<OnlineDetectionHandler>(scope.ServiceProvider));
        handlers.Add(ActivatorUtilities.CreateInstance<StartupHandler>(scope.ServiceProvider));
        handlers.Add(ActivatorUtilities.CreateInstance<ShutdownHandler>(scope.ServiceProvider));
        handlers.Add(ActivatorUtilities.CreateInstance<InstallationHandler>(scope.ServiceProvider));
        handlers.Add(ActivatorUtilities.CreateInstance<DebugHandler>(scope.ServiceProvider));
        
        // Resolve additional components
        var components = new List<IServerComponent>();
        
        components.Add(ActivatorUtilities.CreateInstance<ConsoleSignalRComponent>(scope.ServiceProvider));

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
            handlers,
            components
        );

        context.Server = server;

        return server;
    }
}