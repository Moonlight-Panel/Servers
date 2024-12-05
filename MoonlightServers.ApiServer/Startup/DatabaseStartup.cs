using Moonlight.ApiServer.Helpers;
using Moonlight.ApiServer.Interfaces.Startup;
using MoonlightServers.ApiServer.Database;

namespace MoonlightServers.ApiServer.Startup;

public class DatabaseStartup : IDatabaseStartup
{
    public Task ConfigureDatabase(DatabaseContextCollection collection)
    {
        collection.Add<MoonlightServersDataContext>();

        return Task.CompletedTask;
    }
}