namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes;

public class NodeDetailResponse
{
    public int Id { get; set; }

    public string Name { get; set; }
    
    public string Fqdn { get; set; }
    public string Token { get; set; }
    public int HttpPort { get; set; }
    public int FtpPort { get; set; }
    
    public bool EnableTransparentMode { get; set; }
    public bool EnableDynamicFirewall { get; set; }
}