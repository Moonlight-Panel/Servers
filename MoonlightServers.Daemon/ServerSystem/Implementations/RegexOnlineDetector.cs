using System.Text.RegularExpressions;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Implementations;

public class RegexOnlineDetector : IOnlineDetector
{
    private readonly ServerContext Context;
    
    private Regex? Expression;

    public RegexOnlineDetector(ServerContext context)
    {
        Context = context;
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public Task CreateAsync()
    {
        if(string.IsNullOrEmpty(Context.Configuration.OnlineDetection))
            return Task.CompletedTask;

        Expression = new Regex(Context.Configuration.OnlineDetection, RegexOptions.Compiled);
        
        return Task.CompletedTask;
    }

    public Task<bool> HandleOutputAsync(string line)
    {
        if (Expression == null)
            return Task.FromResult(false);

        var result = Expression.Matches(line).Count > 0;

        return Task.FromResult(result);
    }

    public Task DestroyAsync()
    {
        Expression = null;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Expression = null;
        return ValueTask.CompletedTask;
    }
}