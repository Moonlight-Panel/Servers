using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.ServerSystem;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DefaultRestorer : IRestorer
{
    private readonly ILogger Logger;
    private readonly IConsole Console;
    private readonly IProvisioner Provisioner;
    private readonly IInstaller Installer;
    private readonly IStatistics Statistics;

    public DefaultRestorer(
        ILoggerFactory loggerFactory,
        ServerContext context,
        IConsole console,
        IProvisioner provisioner,
        IStatistics statistics,
        IInstaller installer
    )
    {
        Logger = loggerFactory.CreateLogger($"Servers.Instance.{context.Configuration.Id}.{nameof(DefaultRestorer)}");
        Console = console;
        Provisioner = provisioner;
        Statistics = statistics;
        Installer = installer;
    }

    public Task Initialize()
        => Task.CompletedTask;

    public Task Sync()
        => Task.CompletedTask;

    public async Task<ServerState> Restore()
    {
        Logger.LogDebug("Restoring server state");

        if (Provisioner.IsProvisioned)
        {
            Logger.LogDebug("Detected runtime to restore");

            await Console.CollectFromRuntime();
            await Console.AttachToRuntime();
            await Statistics.SubscribeToRuntime();

            return ServerState.Online;
        }

        if (Installer.IsRunning)
        {
            Logger.LogDebug("Detected installation to restore");

            await Console.CollectFromInstallation();
            await Console.AttachToInstallation();
            await Statistics.SubscribeToInstallation();

            return ServerState.Installing;
        }

        Logger.LogDebug("Nothing found to restore");
        return ServerState.Offline;
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}