namespace MoonlightServers.DaemonShared.Http.Responses.Sys;

public class SystemDataUsageResponse
{
    public long ImagesUsed { get; set; }
    public long ImagesReclaimable { get; set; }
    public long ContainersUsed { get; set; }
    public long ContainersReclaimable { get; set; }
    public long BuildCacheUsed { get; set; }
    public long BuildCacheReclaimable { get; set; }
}