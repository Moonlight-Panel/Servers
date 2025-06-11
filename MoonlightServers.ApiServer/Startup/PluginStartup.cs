using MoonCore.Extensions;
using Moonlight.ApiServer.Configuration;
using Moonlight.ApiServer.Models;
using Moonlight.ApiServer.Plugins;
using MoonlightServers.ApiServer.Database;
using MoonlightServers.ApiServer.Helpers;
using MoonlightServers.ApiServer.Implementations.ServerAuthFilters;
using MoonlightServers.ApiServer.Interfaces;

namespace MoonlightServers.ApiServer.Startup;

[PluginStartup]
public class PluginStartup : IPluginStartup
{
    public Task BuildApplication(IServiceProvider serviceProvider, IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddDbContext<ServersDataContext>();

        // Configure authentication for the remote endpoints
        builder.Services
            .AddAuthentication()
            .AddScheme<NodeAuthOptions, NodeAuthScheme>("nodeAuthentication", null);

        var configuration = serviceProvider.GetRequiredService<AppConfiguration>();

        if (configuration.Client.Enable)
        {
            builder.Services.AddSingleton(new FrontendConfigurationOption()
            {
                Scripts =
                [
                    "js/XtermBlazor.min.js",
                    "js/addon-fit.js",
                    "js/moonlightServers.js"
                ],
                Styles = ["css/XtermBlazor.min.css"]
            });
        }
        
        //  Add server auth filters
        builder.Services.AddSingleton<IServerAuthorizationFilter, OwnerAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, AdminAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, ShareAuthFilter>();

        return Task.CompletedTask;
    }

    public Task ConfigureApplication(IServiceProvider serviceProvider, IApplicationBuilder app)
        => Task.CompletedTask;

    public Task ConfigureEndpoints(IServiceProvider serviceProvider, IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}