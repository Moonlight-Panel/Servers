using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using Moonlight.Client.App.Interfaces;
using Moonlight.Client.App.PluginApi;
using MoonlightServers.Client.Implementations;

namespace MoonlightServers.Client;

public class MoonlightServersClientPlugin : MoonlightClientPlugin
{
    public MoonlightServersClientPlugin(ILogger logger, PluginService pluginService) : base(logger, pluginService)
    {
    }
    
    public override Task OnLoaded()
    {
        PluginService.RegisterImplementation<ISidebarItemProvider, ServersSidebarProvider>();
        
        return Task.CompletedTask;
    }

    public override Task OnAppBuilding(WebAssemblyHostBuilder builder)
    {
        return Task.CompletedTask;
    }

    public override Task OnAppConfiguring(WebAssemblyHost app)
    {
        return Task.CompletedTask;
    }
}