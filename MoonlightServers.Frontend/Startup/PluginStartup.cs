using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Extensions;
using Moonlight.Client.Interfaces;
using MoonlightServers.Frontend.Implementations;
using MoonlightServers.Frontend.Interfaces;

namespace MoonlightServers.Frontend.Startup;

public class PluginStartup : IPluginStartup
{
    public Task BuildApplication(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddSingleton<ISidebarItemProvider, SidebarImplementation>();
        builder.Services.AddSingleton<IServerTabProvider, DefaultServerTabProvider>();
        
        builder.Services.AutoAddServices<PluginStartup>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(WebAssemblyHost app)
    {
        return Task.CompletedTask;
    }
}