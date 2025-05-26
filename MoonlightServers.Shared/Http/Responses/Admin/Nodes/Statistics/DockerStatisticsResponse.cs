namespace MoonlightServers.Shared.Http.Responses.Admin.Nodes.Statistics;

public class DockerStatisticsResponse
{
    public string Version { get; set; }

    public long ImagesUsed { get; set; }
    public long ImagesReclaimable { get; set; }
    
    public long ContainersUsed { get; set; }
    public long ContainersReclaimable { get; set; }
    
    public long BuildCacheUsed { get; set; }
    public long BuildCacheReclaimable { get; set; }
}