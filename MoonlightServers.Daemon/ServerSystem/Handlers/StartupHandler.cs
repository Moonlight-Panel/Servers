using MoonlightServers.Daemon.ServerSystem.Enums;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;
using Stateless;

namespace MoonlightServers.Daemon.ServerSystem.Handlers;

public class StartupHandler : IServerStateHandler
{
    private IAsyncDisposable? ExitSubscription;

    private readonly ServerContext Context;
    private Server Server => Context.Server;

    public StartupHandler(ServerContext context)
    {
        Context = context;
    }

    public async Task ExecuteAsync(StateMachine<ServerState, ServerTrigger>.Transition transition)
    {
        // Filter
        if (transition is not {Source: ServerState.Offline, Destination: ServerState.Starting, Trigger: ServerTrigger.Start})
            return;

        // Plan:
        // 1. Fetch latest configuration
        // 2. Check if file system exists
        // 3. Check if file system is mounted
        // 4. Run file system checks
        // 5. Create runtime
        // 6. Attach console
        // 7. Attach statistics collector
        // 8. Create online detector
        // 9. Start runtime

        // 1. Fetch latest configuration
        // TODO

        // 2. Check if file system exists
        if (!await Server.RuntimeFileSystem.CheckExistsAsync())
            await Server.RuntimeFileSystem.CreateAsync();

        // 3. Check if file system is mounted
        if (!await Server.RuntimeFileSystem.CheckMountedAsync())
            await Server.RuntimeFileSystem.CheckMountedAsync();

        // 4. Run file system checks
        await Server.RuntimeFileSystem.PerformChecksAsync();

        // 5. Create runtime
        var hostPath = await Server.RuntimeFileSystem.GetPathAsync();

        if (await Server.Runtime.CheckExistsAsync())
            await Server.Runtime.DestroyAsync();
        
        await Server.Runtime.CreateAsync(hostPath);

        if (ExitSubscription == null)
        {
            ExitSubscription = await Server.Runtime.SubscribeExited(OnRuntimeExited);
        }

        // 6. Attach console

        await Server.Console.AttachRuntimeAsync();

        // 7. Attach statistics collector

        await Server.Statistics.AttachRuntimeAsync();

        // 8. Create online detector

        await Server.OnlineDetector.CreateAsync();
        await Server.OnlineDetector.DestroyAsync();

        // 9. Start runtime

        await Server.Runtime.StartAsync();
    }

    private async ValueTask OnRuntimeExited(int exitCode)
    {
        // TODO: Notify the crash handler component of the exit code

        await Server.StateMachine.FireAsync(ServerTrigger.Exited);
    }

    public async ValueTask DisposeAsync()
    {
        if (ExitSubscription != null)
            await ExitSubscription.DisposeAsync();
    }
}