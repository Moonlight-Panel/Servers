using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Handlers;

public class InstallationHandler : IServerStateHandler
{
    private readonly ServerContext Context;
    private Server Server => Context.Server;

    private IAsyncDisposable? ExitSubscription;

    public InstallationHandler(ServerContext context)
    {
        Context = context;
    }

    public async Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        if (transition is
            { Source: ServerState.Offline, Destination: ServerState.Installing, Trigger: ServerTrigger.Install })
        {
            await StartAsync();
        }
        else if (transition is
                 { Source: ServerState.Installing, Destination: ServerState.Offline, Trigger: ServerTrigger.Exited })
        {
            await CompleteAsync();
        }
    }

    private async Task StartAsync()
    {
        // Plan:
        // 1. Fetch latest configuration
        // 2. Check if both file systems exists
        // 3. Check if both file systems are mounted
        // 4. Run file system checks
        // 5. Create installation container
        // 6. Attach console
        // 7. Start installation container

        // 1. Fetch latest configuration
        var installData = new ServerInstallDataResponse()
        {
            Script = await File.ReadAllTextAsync(Path.Combine("storage", "install.sh")),
            Shell = "/bin/ash",
            DockerImage = "ghcr.io/parkervcp/installers:alpine"
        };

        // 2. Check if file system exists
        if (!await Server.RuntimeFileSystem.CheckExistsAsync())
            await Server.RuntimeFileSystem.CreateAsync();

        if (!await Server.InstallationFileSystem.CheckExistsAsync())
            await Server.InstallationFileSystem.CreateAsync();

        // 3. Check if both file systems are mounted
        if (!await Server.RuntimeFileSystem.CheckMountedAsync())
            await Server.RuntimeFileSystem.MountAsync();

        if (!await Server.InstallationFileSystem.CheckMountedAsync())
            await Server.InstallationFileSystem.MountAsync();

        // 4. Run file system checks
        await Server.RuntimeFileSystem.PerformChecksAsync();
        await Server.InstallationFileSystem.PerformChecksAsync();

        // 5. Create installation

        var runtimePath = await Server.RuntimeFileSystem.GetPathAsync();
        var installationPath = await Server.InstallationFileSystem.GetPathAsync();

        if (await Server.Installation.CheckExistsAsync())
            await Server.Installation.DestroyAsync();

        await Server.Installation.CreateAsync(runtimePath, installationPath, installData);
        
        if (ExitSubscription == null)
            ExitSubscription = await Server.Installation.SubscribeExited(OnInstallationExited);

        // 6. Attach console

        await Server.Console.AttachInstallationAsync();

        // 7. Start installation container
        await Server.Installation.StartAsync();
    }

    private async ValueTask OnInstallationExited(int exitCode)
    {
        // TODO: Notify the crash handler component of the exit code
        
        await Server.StateMachine.FireAsync(ServerTrigger.Exited);
    }

    private async Task CompleteAsync()
    {
        // Plan:
        // 1. Handle possible crash
        // 2. Remove installation container
        // 3. Remove installation file system

        // 1. Handle possible crash
        // TODO

        // 2. Remove installation container
        await Server.Installation.DestroyAsync();

        // 3. Remove installation file system
        await Server.InstallationFileSystem.UnmountAsync();
        await Server.InstallationFileSystem.DestroyAsync();
        
        Context.Logger.LogDebug("Completed installation");
    }

    public async ValueTask DisposeAsync()
    {
        if (ExitSubscription != null)
            await ExitSubscription.DisposeAsync();
    }
}