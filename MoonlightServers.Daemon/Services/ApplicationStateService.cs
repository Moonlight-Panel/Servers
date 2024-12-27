using MoonCore.Attributes;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class ApplicationStateService : IHostedLifecycleService
{
    private readonly ServerService ServerService;
    private readonly ILogger<ApplicationStateService> Logger;

    public ApplicationStateService(ServerService serverService, ILogger<ApplicationStateService> logger)
    {
        ServerService = serverService;
        Logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
     => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Performing initialization");
        
        await ServerService.Initialize();
    }

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping services");

        await ServerService.Stop();
    }
}