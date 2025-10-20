using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Moonlight.Client.Startup;
using MoonlightServers.Frontend.Runtime;

var pluginLoader = new DevPluginLoader();
pluginLoader.Initialize();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.AddMoonlight(pluginLoader.Instances);

var app = builder.Build();

app.ConfigureMoonlight(pluginLoader.Instances);

await app.RunAsync();