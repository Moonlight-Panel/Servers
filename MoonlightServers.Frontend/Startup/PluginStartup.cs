using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MoonCore.Extensions;
using MoonCore.PluginFramework.Extensions;
using Moonlight.Client.Interfaces;
using MoonlightServers.Frontend.Interfaces;

namespace MoonlightServers.Frontend.Startup;

public class PluginStartup : IAppStartup
{
    public Task BuildApp(WebAssemblyHostBuilder builder)
    {
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddInterfaces(configuration =>
        {
            configuration.AddAssembly(GetType().Assembly);

            configuration.AddInterface<IServerTabProvider>();
        });
        
        return Task.CompletedTask;
    }

    public Task ConfigureApp(WebAssemblyHost app)
    {
        return Task.CompletedTask;
    }
}