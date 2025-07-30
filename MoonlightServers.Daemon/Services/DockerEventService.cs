using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Observability;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.Services;

public class DockerEventService : BackgroundService
{
    private readonly ILogger<DockerEventService> Logger;
    private readonly DockerClient DockerClient;

    public IAsyncObservable<Message> OnContainerEvent => OnContainerSubject;
    public IAsyncObservable<Message> OnImageEvent => OnImageSubject;
    public IAsyncObservable<Message> OnNetworkEvent =>  OnNetworkSubject;
    
    private readonly EventSubject<Message> OnContainerSubject = new();
    private readonly EventSubject<Message> OnImageSubject = new();
    private readonly EventSubject<Message> OnNetworkSubject = new();

    public DockerEventService(
        ILogger<DockerEventService> logger,
        DockerClient dockerClient
    )
    {
        Logger = logger;
        DockerClient = dockerClient;
    }

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
                                    await OnContainerSubject.OnNextAsync(message);
                                    break;

                                case "image":
                                    await OnImageSubject.OnNextAsync(message);
                                    break;

                                case "network":
                                    await OnNetworkSubject.OnNextAsync(message);
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

    public override void Dispose()
    {
        base.Dispose();
        
        OnContainerSubject.Dispose();
        OnImageSubject.Dispose();
        OnNetworkSubject.Dispose();
    }
}