using System.Net.Sockets;
using System.Text.Json;
using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Models.UnsafeDocker;

namespace MoonlightServers.Daemon.Helpers;

[Singleton]
public class UnsafeDockerClient
{
    private readonly AppConfiguration Configuration;

    public UnsafeDockerClient(AppConfiguration configuration)
    {
        Configuration = configuration;
    }

    public Task<HttpClient> CreateHttpClient()
    {
        var client = new HttpClient(new SocketsHttpHandler()
        {
            ConnectCallback = async (context, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(
                    Formatter.ReplaceStart(Configuration.Docker.Uri, "unix://", "")
                );
                await socket.ConnectAsync(endpoint, token);
                return new NetworkStream(socket, ownsSocket: true);
            }
        });

        return Task.FromResult(client);
    }

    public async Task<DataUsageResponse> GetDataUsage()
    {
        using var client = await CreateHttpClient();
        var responseJson = await client.GetStringAsync("http://some.random.domain/v1.47/system/df");
        var response = JsonSerializer.Deserialize<DataUsageResponse>(responseJson)!;

        return response;
    }
}