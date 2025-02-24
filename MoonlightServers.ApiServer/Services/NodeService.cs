using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Attributes;
using MoonCore.Extended.Helpers;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Sys;

namespace MoonlightServers.ApiServer.Services;

[Singleton]
public class NodeService
{
    public async Task<HttpApiClient> CreateApiClient(Node node)
    {
        var url = "";

        if (node.UseSsl)
            url += "https://";
        else
            url += "http://";

        url += $"{node.Fqdn}:{node.HttpPort}/";

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(url)
        };

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {node.Token}");

        return new HttpApiClient(httpClient);
    }

    public string CreateAccessToken(Node node, Action<Dictionary<string, object>> parameters, TimeSpan duration)
    {
        var claims = new Dictionary<string, object>();
        parameters.Invoke(claims);

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityTokenDescriptor = new SecurityTokenDescriptor()
        {
            Expires = DateTime.UtcNow.Add(duration),
            NotBefore = DateTime.UtcNow.AddSeconds(-1),
            Claims = claims,
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    node.Token
                )),
                SecurityAlgorithms.HmacSha256
            )
        };

        var securityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);

        return jwtSecurityTokenHandler.WriteToken(securityToken);
    }

    public async Task<SystemStatusResponse> GetSystemStatus(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<SystemStatusResponse>("api/system/status");
    }

    #region Statistics

    public async Task<StatisticsApplicationResponse> GetApplicationStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsApplicationResponse>("api/statistics/application");
    }

    public async Task<StatisticsHostResponse> GetHostStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsHostResponse>("api/statistics/host");
    }

    public async Task<StatisticsDockerResponse> GetDockerStatistics(Node node)
    {
        using var apiClient = await CreateApiClient(node);
        return await apiClient.GetJson<StatisticsDockerResponse>("api/statistics/docker");
    }

    #endregion
}