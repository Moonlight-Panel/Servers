namespace MoonlightServers.ApiServer.Database.Entities;

public class Allocation
{
    public int Id { get; set; }

    public string IpAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; }

    public Server? Server { get; set; }
    public Node Node { get; set; }
}