using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Http.Hubs;
using MoonlightServers.Daemon.Models;
using MoonlightServers.Daemon.Models.Cache;
using Stateless;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    // Exposed configuration/state values
    public int Id => Configuration.Id;
    public ServerState State => StateMachine.State;

    // Exposed container names and ids
    public string RuntimeContainerName { get; private set; }
    public string? RuntimeContainerId { get; private set; }

    public string InstallationContainerName { get; private set; }
    public string? InstallationContainerId { get; private set; }

    // Events
    public event Func<ServerState, Task>? OnStateChanged;
    public event Func<string, Task>? OnConsoleOutput;

    // Private stuff

    private readonly ILogger Logger;
    private readonly IServiceProvider ServiceProvider;
    private readonly ServerConsole Console;

    private readonly IHubContext<ServerWebSocketHub> WebSocketHub;

    private StateMachine<ServerState, ServerTrigger> StateMachine;
    private ServerConfiguration Configuration;
    private CancellationTokenSource Cancellation;

    public Server(
        ILogger logger,
        IServiceProvider serviceProvider,
        ServerConfiguration configuration,
        IHubContext<ServerWebSocketHub> webSocketHub
    )
    {
        Logger = logger;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        WebSocketHub = webSocketHub;

        Console = new();
        Cancellation = new();

        RuntimeContainerName = $"moonlight-runtime-{Configuration.Id}";
        InstallationContainerName = $"moonlight-install-{Configuration.Id}";
    }

    public void UpdateConfiguration(ServerConfiguration configuration)
        => Configuration = configuration;
}