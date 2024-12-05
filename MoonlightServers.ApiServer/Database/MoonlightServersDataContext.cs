using Moonlight.ApiServer.Configuration;
using Moonlight.ApiServer.Helpers;

namespace MoonlightServers.ApiServer.Database;

public class MoonlightServersDataContext : DatabaseContext
{
    public override string Prefix { get; } = "MoonlightServers";

    public MoonlightServersDataContext(AppConfiguration configuration) : base(configuration)
    {
    }
}