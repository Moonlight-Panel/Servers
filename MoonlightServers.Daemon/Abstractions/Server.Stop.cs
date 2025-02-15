using MoonlightServers.Daemon.Enums;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    public async Task Stop() => await StateMachine.FireAsync(ServerTrigger.Stop);

    private async Task InternalStop()
    {
        try
        {
            await Console.WriteToInput($"{Configuration.StopCommand}\n\r");
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while performing stop trigger: {e}", e);
            await StateMachine.FireAsync(ServerTrigger.NotifyInternalError);
        }
    }

    private async Task InternalFinishStop()
    {
        await Destroy();
    }
}