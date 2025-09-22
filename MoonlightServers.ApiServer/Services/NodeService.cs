using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoonCore.Attributes;
using MoonCore.Helpers;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Statistics;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Sys;

namespace MoonlightServers.ApiServer.Services;

[Singleton]
public class NodeService
{
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
            ),
            Audience = node.TokenId
        };

        var securityToken = jwtSecurityTokenHandler.CreateJwtSecurityToken(securityTokenDescriptor);

        return jwtSecurityTokenHandler.WriteToken(securityToken);
    }

    public async Task<SystemStatusResponse> GetSystemStatusAsync(Node node)
    {
        using var apiClient = CreateApiClient(node);
        return await apiClient.GetJson<SystemStatusResponse>("api/system/status");
    }

    #region Statistics

    public async Task<StatisticsResponse> GetStatisticsAsync(Node node)
    {
        using var apiClient = CreateApiClient(node);
        return await apiClient.GetJson<StatisticsResponse>("api/statistics");
    }

    public async Task<StatisticsDockerResponse> GetDockerStatisticsAsync(Node node)
    {
        using var apiClient = CreateApiClient(node);
        return await apiClient.GetJson<StatisticsDockerResponse>("api/statistics/docker");
    }

    #endregion

    #region Helpers
    
    public HttpApiClient CreateApiClient(Node node)
    {
        var url = "";

        url += node.UseSsl ? "https://" : "http://";
        url += $"{node.Fqdn}:{node.HttpPort}/";

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(url)
        };

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {node.Token}");

        return new HttpApiClient(httpClient);
    }

    #endregion
}