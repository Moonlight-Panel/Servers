using Docker.DotNet;
using MoonlightServers.Daemon.Enums;
using MoonlightServers.Daemon.Extensions;
using MoonlightServers.Daemon.Services;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Start() => await StateMachine.FireAsync(ServerTrigger.Start);
    
    private async Task InternalStart()
    {
        try
        {
            await LogToConsole("Fetching configuration");
            
            var remoteService = ServiceProvider.GetRequiredService<RemoteService>();
            var serverData = await remoteService.GetServer(Configuration.Id);
            
            // We are updating the server config here
            // as changes to variables and other settings wouldn't sync otherwise
            // because they won't trigger a sync
            var serverConfiguration = serverData.ToServerConfiguration();
            UpdateConfiguration(serverConfiguration);
            
            await ReCreate();

            await LogToConsole("Starting container");

            // We can disable the null check for the runtime container id, as we set it by calling ReCreate();
            await AttachConsole(RuntimeContainerId!);
            
            // Start container
            var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();
            await dockerClient.Containers.StartContainerAsync(RuntimeContainerId, new());
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while performing start trigger: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.NotifyInternalError);
        }
    }
}