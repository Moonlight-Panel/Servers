using Moonlight.Client;

// Development Client Startup

// This file is a small helper for development instances for moonlight.
// It calls the moonlight startup with the current project loaded as a plugin.
// This allows you to develop and debug projects without any hassle

// !!! DO NOT HARDCORE ANY SECRETS HERE !!!

var startup = new Startup();

await startup.Run(args, [
    typeof(Program).Assembly
]);