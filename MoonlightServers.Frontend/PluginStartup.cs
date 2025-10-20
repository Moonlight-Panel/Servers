using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MoonCore.Extensions;
using Moonlight.Client.Interfaces;
using Moonlight.Client.Plugins;
using MoonlightServers.Frontend.Implementations;
using MoonlightServers.Frontend.Interfaces;

namespace MoonlightServers.Frontend;

public class PluginStartup : IPluginStartup
{
    public void AddPlugin(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddSingleton<ISidebarItemProvider, SidebarImplementation>();
        builder.Services.AddSingleton<IServerTabProvider, DefaultServerTabProvider>();
        builder.Services.AddSingleton<IServerPermissionProvider, DefaultPermissionProvider>();
        
        builder.Services.AutoAddServices<PluginStartup>();
    }

    public void ConfigurePlugin(WebAssemblyHost app)
    {
        
    }
}