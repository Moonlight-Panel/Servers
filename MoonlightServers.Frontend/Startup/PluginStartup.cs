using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Extensions;
using Moonlight.Client.Interfaces;
using Moonlight.Client.Plugins;
using MoonlightServers.Frontend.Implementations;
using MoonlightServers.Frontend.Interfaces;

namespace MoonlightServers.Frontend.Startup;

[PluginStartup]
public class PluginStartup : IPluginStartup
{
    public Task BuildApplication(IServiceProvider serviceProvider, WebAssemblyHostBuilder builder)
    {
        builder.Services.AddSingleton<ISidebarItemProvider, SidebarImplementation>();
        builder.Services.AddSingleton<IServerTabProvider, DefaultServerTabProvider>();
        
        builder.Services.AutoAddServices<PluginStartup>();
        
        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IServiceProvider serviceProvider, WebAssemblyHost app)
    {
        return Task.CompletedTask;
    }
}