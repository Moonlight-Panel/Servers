using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MoonCore.Extensions;
using Moonlight.ApiServer.Configuration;
using Moonlight.ApiServer.Models;
using Moonlight.ApiServer.Plugins;
using MoonlightServers.ApiServer.Database;
using MoonlightServers.ApiServer.Helpers;
using MoonlightServers.ApiServer.Implementations.ServerAuthFilters;
using MoonlightServers.ApiServer.Interfaces;

namespace MoonlightServers.ApiServer.Startup;

public class PluginStartup : IPluginStartup
{
    public Task BuildApplicationAsync(IServiceProvider serviceProvider, IHostApplicationBuilder builder)
    {
        // Scan the current plugin assembly for di services
        builder.Services.AutoAddServices<PluginStartup>();

        builder.Services.AddDbContext<ServersDataContext>();

        // Configure authentication for the remote endpoints
        builder.Services
            .AddAuthentication()
            .AddScheme<NodeAuthOptions, NodeAuthScheme>("nodeAuthentication", null);

        var configuration = serviceProvider.GetRequiredService<AppConfiguration>();

        if (configuration.Frontend.EnableHosting)
        {
            builder.Services.AddSingleton(new FrontendConfigurationOption()
            {
                Scripts =
                [
                    "/_content/MoonlightServers.Frontend/js/XtermBlazor.min.js",
                    "/_content/MoonlightServers.Frontend/js/addon-fit.js",
                    "/_content/MoonlightServers.Frontend/js/moonlightServers.js"
                ],
                Styles = ["/_content/MoonlightServers.Frontend/css/XtermBlazor.min.css"]
            });
        }
        
        //  Add server auth filters
        builder.Services.AddSingleton<IServerAuthorizationFilter, OwnerAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, AdminAuthFilter>();
        builder.Services.AddScoped<IServerAuthorizationFilter, ShareAuthFilter>();

        return Task.CompletedTask;
    }

    public Task ConfigureApplicationAsync(IServiceProvider serviceProvider, IApplicationBuilder app)
        => Task.CompletedTask;

    public Task ConfigureEndpointsAsync(IServiceProvider serviceProvider, IEndpointRouteBuilder routeBuilder)
        => Task.CompletedTask;
}