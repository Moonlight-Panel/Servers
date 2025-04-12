namespace MoonlightServers.ApiServer.Services;

public class NodeBootService : IHostedLifecycleService
{
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        // TODO: Add node boot calls here
    }

    #region Unused

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    #endregion
}