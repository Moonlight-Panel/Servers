namespace MoonlightServers.Shared.Http.Responses.Client.Servers.Files;

public class ServerFilesEntryResponse
{
    public string Name { get; set; }
    public bool IsFolder { get; set; }
    public long Size { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}