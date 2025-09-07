using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Implementations;

public class ServerReporter : IReporter
{
    private readonly ServerContext Context;
    
    private const string StatusTemplate =
        "\x1b[1;38;2;200;90;200mM\x1b[1;38;2;204;110;230mo\x1b[1;38;2;170;130;245mo\x1b[1;38;2;140;150;255mn\x1b[1;38;2;110;180;255ml\x1b[1;38;2;100;200;255mi\x1b[1;38;2;100;220;255mg\x1b[1;38;2;120;235;255mh\x1b[1;38;2;140;250;255mt\x1b[0m \x1b[3;38;2;200;200;200m{0}\x1b[0m\n\r";

    private const string ErrorTemplate =
        "\x1b[1;38;2;200;90;200mM\x1b[1;38;2;204;110;230mo\x1b[1;38;2;170;130;245mo\x1b[1;38;2;140;150;255mn\x1b[1;38;2;110;180;255ml\x1b[1;38;2;100;200;255mi\x1b[1;38;2;100;220;255mg\x1b[1;38;2;120;235;255mh\x1b[1;38;2;140;250;255mt\x1b[0m \x1b[1;38;2;255;0;0m{0}\x1b[0m\n\r";
    
    public ServerReporter(ServerContext context)
    {
        Context = context;
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public async Task StatusAsync(string message)
    {
        Context.Logger.LogInformation("Status: {message}", message);
        
        await Context.Server.Console.WriteStdOutAsync(
            string.Format(StatusTemplate, message)
        );
    }

    public async Task ErrorAsync(string message)
    {
        Context.Logger.LogError("Error: {message}", message);
        
        await Context.Server.Console.WriteStdOutAsync(
            string.Format(ErrorTemplate, message)
        );
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;
}