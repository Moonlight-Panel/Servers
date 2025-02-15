using Docker.DotNet;
using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Start() => await StateMachine.FireAsync(ServerTrigger.Start);
    
    private async Task InternalStart()
    {
        try
        {
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