using System.Text.Json;
using MoonCore.Attributes;
using MoonCore.Extended.Helpers;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class AccessTokenHelper
{
    private readonly AppConfiguration Configuration;

    public AccessTokenHelper(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    public bool Process(string accessToken, out Dictionary<string, JsonElement> data)
    {
        return JwtHelper.TryVerifyAndDecodePayload(Configuration.Security.Token, accessToken, out data);
    }
}