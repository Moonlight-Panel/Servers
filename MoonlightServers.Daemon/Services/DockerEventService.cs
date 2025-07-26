using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MoonlightServers.Daemon.Services;

public class DockerEventService : BackgroundService
{
    private readonly ILogger<DockerEventService> Logger;
    private readonly DockerClient DockerClient;

    public IObservable<Message> OnContainerEvent => OnContainerSubject;
    public IObservable<Message> OnImageEvent => OnImageSubject;
    public IObservable<Message> OnNetworkEvent =>  OnNetworkSubject;
    
    private readonly Subject<Message> OnContainerSubject = new();
    private readonly Subject<Message> OnImageSubject = new();
    private readonly Subject<Message> OnNetworkSubject = new();

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
                    new Progress<Message>(message =>
                    {
                        switch (message.Type)
                        {
                            case "container":
                                OnContainerSubject.OnNext(message);
                                break;

                            case "image":
                                OnImageSubject.OnNext(message);
                                break;

                            case "network":
                                OnNetworkSubject.OnNext(message);
                                break;
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