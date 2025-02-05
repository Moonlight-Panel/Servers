using Moonlight.Client;

var startup = new Startup();

await startup.Run(args, [
    typeof(Program).Assembly
]);