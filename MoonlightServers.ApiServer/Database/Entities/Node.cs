namespace MoonlightServers.ApiServer.Database.Entities;

public class Node
{
    public int Id { get; set; }
    
    // Relations
    public List<Server> Servers { get; set; } = new();
    public List<Allocation> Allocations { get; set; } = new();

    // Meta
    public string Name { get; set; }
    
    // Connection details
    public string Fqdn { get; set; }
    public string Token { get; set; }
    public int HttpPort { get; set; }
    public int FtpPort { get; set; }
    
    // Misc
    public bool EnableTransparentMode { get; set; }
    public bool EnableDynamicFirewall { get; set; }
}