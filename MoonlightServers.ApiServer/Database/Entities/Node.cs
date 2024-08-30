namespace MoonlightServers.ApiServer.Database.Entities;

public class Node
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Fqdn { get; set; }
    public int ApiPort { get; set; }
    public string Token { get; set; }
    public bool SslEnabled { get; set; }

    public List<Allocation> Allocations { get; set; } = new();
    public List<Server> Servers { get; set; } = new();
}