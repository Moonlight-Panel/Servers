using Docker.DotNet;
using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Kill() => await StateMachine.FireAsync(ServerTrigger.Kill);

    private async Task InternalKill()
    {
        try
        {
            if (RuntimeContainerId == null)
                return;

            await LogToConsole("Killing container");
            
            var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();
            await dockerClient.Containers.KillContainerAsync(RuntimeContainerId, new());
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while performing stop trigger: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.NotifyInternalError);
        }
    }
}