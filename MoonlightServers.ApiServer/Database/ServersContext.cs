using Moonlight.ApiServer.App.Helpers.Database;

namespace MoonlightServers.ApiServer.Database;

public class ServersContext : DatabaseContext
{
    public override string Prefix => "Servers";
}