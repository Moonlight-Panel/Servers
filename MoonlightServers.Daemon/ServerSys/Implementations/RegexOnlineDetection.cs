using System.Text.RegularExpressions;
using MoonCore.Observability;
using MoonlightServers.Daemon.ServerSys.Abstractions;
using MoonlightServers.Daemon.ServerSystem;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class RegexOnlineDetection : IOnlineDetection
{
    private readonly ServerContext Context;
    private readonly IConsole Console;
    private readonly ILogger<RegexOnlineDetection> Logger;
    
    private Regex? Regex;
    private IAsyncDisposable? ConsoleSubscription;
    private IAsyncDisposable? StateSubscription;

    public RegexOnlineDetection(ServerContext context, IConsole console, ILogger<RegexOnlineDetection> logger)
    {
        Context = context;
        Console = console;
        Logger = logger;
    }

    public async Task Initialize()
    {
        Logger.LogInformation("Subscribing to state changes");
        
        StateSubscription = await Context.Self.OnState.SubscribeAsync(async state =>
        {
            if (state == ServerState.Starting) // Subscribe to console when starting
            {
                Logger.LogInformation("Detected state change to online. Subscribing to console in order to check for the regex matches");
                
                if(ConsoleSubscription != null)
                    await ConsoleSubscription.DisposeAsync();

                try
                {
                    Regex = new(Context.Configuration.OnlineDetection, RegexOptions.Compiled);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "An error occured while building regex expression. Please make sure the regex is valid");
                }
                
                ConsoleSubscription = await Console.OnOutput.SubscribeEventAsync(HandleOutput);
            }
            else if (ConsoleSubscription != null) // Unsubscribe from console when any other state and not already unsubscribed
            {
                Logger.LogInformation("Detected state change to {state}. Unsubscribing from console", state);
                
                await ConsoleSubscription.DisposeAsync();
                ConsoleSubscription = null;
            }
        });
    }

    private async ValueTask HandleOutput(string line)
    {
        // Handle here just to make sure. Shouldn't be required as we
        // unsubscribe from the console, as soon as we go online (or any other state).
        // The regex should also not be null as we initialize it in the handler above but whatevers
        if(Context.Self.StateMachine.State != ServerState.Starting || Regex == null)
            return;
        
        if(Regex.Matches(line).Count == 0)
            return;

        await Context.Self.StateMachine.FireAsync(ServerTrigger.OnlineDetected);
    }

    public Task Sync()
    {
        Regex = new(Context.Configuration.OnlineDetection, RegexOptions.Compiled);
        return Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        if(ConsoleSubscription != null)
            await ConsoleSubscription.DisposeAsync();
        
        if(StateSubscription != null)
            await StateSubscription.DisposeAsync();
    }
}