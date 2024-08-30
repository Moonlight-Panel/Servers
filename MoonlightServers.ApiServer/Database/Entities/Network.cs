namespace MoonlightServers.ApiServer.Database.Entities;

public class Network
{
    public int Id { get; set; }

    public string Name { get; set; }

    public Node Node { get; set; }
    public List<Server> Servers { get; set; } = new();
}