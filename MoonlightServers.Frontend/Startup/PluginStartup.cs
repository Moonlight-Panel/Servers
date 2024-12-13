using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Extensions;
using Moonlight.Client.Interfaces;

namespace MoonlightServers.Frontend.Startup;

public class PluginStartup : IAppStartup
{
    public Task BuildApp(WebAssemblyHostBuilder builder)
    {
        builder.Services.AutoAddServices<PluginStartup>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureApp(WebAssemblyHost app)
    {
        return Task.CompletedTask;
    }
}