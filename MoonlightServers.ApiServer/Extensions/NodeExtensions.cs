using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;

namespace MoonlightServers.ApiServer.Extensions;

public static class NodeExtensions
{
    public static HttpApiClient CreateClient(this Node node)
    {
        var httpClient = new HttpClient(new HttpClientHandler() // TODO: Make global http config for proxy etc
        {
            UseProxy = false
        });
        
        var url = $"{(node.SslEnabled ? "https" : "http")}://{node.Fqdn}:{node.ApiPort}/";
        httpClient.BaseAddress = new Uri(url);
        
        httpClient.DefaultRequestHeaders.Add("Authorization", node.Token);

        return new HttpApiClient(httpClient);
    }
}