using MoonCore.Blazor.Tailwind.Forms;
using MoonCore.Blazor.Tailwind.Forms.Components;
using Moonlight.Client;

FormComponentRepository.Set<bool, SwitchComponent>();

var startup = new Startup();

await startup.Run(args, [
    typeof(Program).Assembly
]);