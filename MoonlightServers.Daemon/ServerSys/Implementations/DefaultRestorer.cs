using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.ServerSystem;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DefaultRestorer : IRestorer
{
    private readonly ILogger<DefaultRestorer> Logger;
    private readonly IConsole Console;
    private readonly IProvisioner Provisioner;
    private readonly IInstaller Installer;
    private readonly IStatistics Statistics;

    public DefaultRestorer(
        ILogger<DefaultRestorer> logger,
        IConsole console,
        IProvisioner provisioner,
        IStatistics statistics, IInstaller installer)
    {
        Logger = logger;
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

            await Console.AttachToRuntime();
            await Statistics.SubscribeToRuntime();

            // TODO: Read out existing container log in order to search if the server is online
            
            return ServerState.Online;
        }

        if (Installer.IsRunning)
        {
            Logger.LogDebug("Detected installation to restore");

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