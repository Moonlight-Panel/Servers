using System.Text.RegularExpressions;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class OnlineDetectionService : ServerSubSystem
{
    // We are compiling the regex when the first output has been received
    // and resetting it after the server has stopped to maximize the performance
    // but allowing the startup detection string to change :>
    
    private Regex? CompiledRegex = null;
    
    public OnlineDetectionService(Server server, ILogger logger) : base(server, logger)
    {
        
    }

    public override Task Initialize()
    {
        var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

        consoleSubSystem.OnOutput += async line =>
        {
            if(StateMachine.State != ServerState.Starting)
                return;

            if (CompiledRegex == null)
                CompiledRegex = new Regex(Configuration.OnlineDetection, RegexOptions.Compiled);
            
            if (Regex.Matches(line, Configuration.OnlineDetection).Count == 0)
                return;
            
            await StateMachine.FireAsync(ServerTrigger.OnlineDetected);
        };

        StateMachine.Configure(ServerState.Offline)
            .OnEntryAsync(_ =>
            {
                CompiledRegex = null;
                return Task.CompletedTask;
            });
        
        return Task.CompletedTask;
    }
}