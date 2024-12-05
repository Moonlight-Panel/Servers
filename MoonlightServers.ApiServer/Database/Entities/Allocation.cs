namespace MoonlightServers.ApiServer.Database.Entities;

public class Allocation
{
    public int Id { get; set; }
    
    // Relations
    public Node Node { get; set; }
    public Server? Server { get; set; }

    public string IpAddress { get; set; }
    public int Port { get; set; }
}