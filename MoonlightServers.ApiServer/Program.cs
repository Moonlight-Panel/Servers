using System.Text.Json;
using Moonlight.ApiServer;
using Moonlight.ApiServer.Models;

// Development Server Startup

// This file is a small helper for development instances for moonlight.
// It calls the moonlight startup with the current project loaded as a plugin.
// This allows you to develop and debug projects without any hassle

// !!! DO NOT HARDCORE ANY SECRETS HERE !!!

var startup = new Startup();

#region Creating virtual plugin manifest from plugin.json file

// Read out content
var pluginManifestJson = await File.ReadAllTextAsync("../plugin.json");

// Parse to model
var pluginManifest = JsonSerializer.Deserialize<PluginManifest>(pluginManifestJson, new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true
})!;

// Clear assemblies as we are loading them using the additional assembly parameter
pluginManifest.Assemblies.Clear();

#endregion

await startup.Run(
    args,
    [typeof(Program).Assembly],
    [pluginManifest]
);