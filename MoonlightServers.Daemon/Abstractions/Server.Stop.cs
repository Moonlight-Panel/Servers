using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Stop() => await StateMachine.FireAsync(ServerTrigger.Stop);

    private async Task InternalStop()
    {
        await Console.WriteToInput($"{Configuration.StopCommand}\n\r");
    }

    private async Task InternalFinishStop()
    {
        await Destroy();
    }
}