namespace MoonlightServers.ApiServer.Database.Entities;

public class ServerVariable
{
    public int Id { get; set; }
    
    // Relations
    public Server Server { get; set; }
    
    public string Key { get; set; }
    public string Value { get; set; }
}