namespace MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

public class ServerFileSystemResponse
{
    public string Name { get; set; }
    public bool IsFolder { get; set; }
    public long Size { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}