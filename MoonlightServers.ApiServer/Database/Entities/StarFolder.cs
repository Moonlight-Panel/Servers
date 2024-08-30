namespace MoonlightServers.ApiServer.Database.Entities;

public class StarFolder
{
    public int Id { get; set; }

    public string Name { get; set; }

    public List<Star> Stars { get; set; } = new();
}