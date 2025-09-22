using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Events;

namespace MoonlightServers.Daemon.Services;

public class DockerEventService : BackgroundService
{
    private readonly ILogger<DockerEventService> Logger;
    private readonly DockerClient DockerClient;

    private readonly EventSource<Message> ContainerSource = new();
    private readonly EventSource<Message> ImageSource = new();
    private readonly EventSource<Message> NetworkSource = new();

    public DockerEventService(
        ILogger<DockerEventService> logger,
        DockerClient dockerClient
    )
    {
        Logger = logger;
        DockerClient = dockerClient;
    }

    public async ValueTask<IAsyncDisposable> SubscribeContainerAsync(Func<Message, ValueTask> callback)
        => await ContainerSource.SubscribeAsync(callback);
    
    public async ValueTask<IAsyncDisposable> SubscribeImageAsync(Func<Message, ValueTask> callback)
        => await ImageSource.SubscribeAsync(callback);
    
    public async ValueTask<IAsyncDisposable> SubscribeNetworkAsync(Func<Message, ValueTask> callback)
        => await NetworkSource.SubscribeAsync(callback);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Starting docker event service");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DockerClient.System.MonitorEventsAsync(
                    new ContainerEventsParameters(),
                    new Progress<Message>(async message =>
                    {
                        try
                        {
                            switch (message.Type)
                            {
                                case "container":
                                    await ContainerSource.InvokeAsync(message);
                                    break;

                                case "image":
                                    await ImageSource.InvokeAsync(message);
                                    break;

                                case "network":
                                    await NetworkSource.InvokeAsync(message);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "An error occured while processing docker event");
                        }
                    }),
                    stoppingToken
                );
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Logger.LogError(e, "An error occured while listening for docker events: {message}", e.Message);
            }
        }

        Logger.LogInformation("Stopping docker event service");
    }
}