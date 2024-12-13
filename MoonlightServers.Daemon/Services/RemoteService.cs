using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class RemoteService
{
    private readonly AppConfiguration Configuration;

    public RemoteService(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    public Task<HttpApiClient> CreateHttpClient()
    {
        var formattedUrl = Configuration.Remote.Url.EndsWith('/')
            ? Configuration.Remote.Url
            : Configuration.Remote.Url + "/";
        
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(formattedUrl)
        };
        
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Configuration.Security.Token}");

        var apiClient = new HttpApiClient(httpClient);

        return Task.FromResult(apiClient);
    }

    public async Task GetStatus()
    {
        using var apiClient = await CreateHttpClient();
        await apiClient.Get("api/servers/remote/node/trip");
    }
}