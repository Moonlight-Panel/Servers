using Docker.DotNet;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class ShutdownSubSystem : ServerSubSystem
{
    private readonly DockerClient DockerClient;

    public ShutdownSubSystem(
        Server server,
        ILogger logger,
        DockerClient dockerClient
    ) : base(server, logger)
    {
        DockerClient = dockerClient;
    }

    public override Task Initialize()
    {
        StateMachine.Configure(ServerState.Stopping)
            .OnEntryFromAsync(ServerTrigger.Stop, HandleStop)
            .OnEntryFromAsync(ServerTrigger.Kill, HandleKill);

        return Task.CompletedTask;
    }

    #region Stopping

    private async Task HandleStop()
    {
        try
        {
            await Stop();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while stopping container: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.FailSafe);
        }
    }

    private async Task Stop()
    {
        var provisionSubSystem = Server.GetRequiredSubSystem<ProvisionSubSystem>();

        // Handle signal stopping
        if (Configuration.StopCommand.StartsWith('^'))
        {
            await DockerClient.Containers.KillContainerAsync(provisionSubSystem.CurrentContainerId, new()
            {
                Signal = Configuration.StopCommand.Replace("^", "")
            });
        }
        else // Handle input stopping
        {
            var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();
            await consoleSubSystem.WriteInput($"{Configuration.StopCommand}\n\r");
        }
    }

    #endregion

    private async Task HandleKill()
    {
        try
        {
            await Kill();
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while killing container: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.FailSafe);
        }
    }

    private async Task Kill()
    {
        var provisionSubSystem = Server.GetRequiredSubSystem<ProvisionSubSystem>();

        await DockerClient.Containers.KillContainerAsync(
            provisionSubSystem.CurrentContainerId,
            new()
        );
    }
}