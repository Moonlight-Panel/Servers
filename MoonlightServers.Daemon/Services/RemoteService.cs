using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonCore.Models;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.DaemonShared.PanelSide.Http.Responses;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class RemoteService
{
    private readonly HttpApiClient ApiClient;

    public RemoteService(AppConfiguration configuration)
    {
        ApiClient = CreateHttpClient(configuration);
    }

    public async Task GetStatus()
    {
        await ApiClient.Get("api/remote/servers/node/trip");
    }

    public async Task<PagedData<ServerDataResponse>> GetServers(int page, int perPage)
    {
        return await ApiClient.GetJson<PagedData<ServerDataResponse>>(
            $"api/remote/servers?page={page}&pageSize={perPage}"
        );
    }

    public async Task<ServerDataResponse> GetServer(int serverId)
    {
        return await ApiClient.GetJson<ServerDataResponse>(
            $"api/remote/servers/{serverId}"
        );
    }

    public async Task<ServerInstallDataResponse> GetServerInstallation(int serverId)
    {
        return await ApiClient.GetJson<ServerInstallDataResponse>(
            $"api/remote/servers/{serverId}/install"
        );
    }

    #region Helpers

    private HttpApiClient CreateHttpClient(AppConfiguration configuration)
    {
        var formattedUrl = configuration.Remote.Url.EndsWith('/')
            ? configuration.Remote.Url
            : configuration.Remote.Url + "/";

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(formattedUrl)
        };

        var jwt = GenerateJwt(configuration);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        return new HttpApiClient(httpClient);
    }

    private string GenerateJwt(AppConfiguration configuration)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityTokenDesc = new SecurityTokenDescriptor()
        {
            Expires = DateTime.UtcNow.AddYears(1), // TODO: Document somewhere
            IssuedAt = DateTime.UtcNow,
            Issuer = configuration.Security.TokenId,
            Audience = configuration.Remote.Url,
            NotBefore = DateTime.UtcNow.AddSeconds(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration.Security.Token)
                ),
                SecurityAlgorithms.HmacSha256
            )
        };

        var securityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(securityTokenDesc);

        securityToken.Header.Add("kid", configuration.Security.TokenId);

        return jwtSecurityTokenHandler.WriteToken(securityToken);
    }

    #endregion
}