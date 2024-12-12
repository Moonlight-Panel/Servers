using System.Text.Json.Serialization;

namespace MoonlightServers.Daemon.Models.UnsafeDocker;

public class DataUsageResponse
{
    [JsonPropertyName("BuildCache")]
    public BuildCacheData[] BuildCache { get; set; }
    
    [JsonPropertyName("LayersSize")]
    public long LayersSize { get; set; }

    [JsonPropertyName("Images")]
    public ImageData[] Images { get; set; }

    [JsonPropertyName("Containers")]
    public ContainerData[] Containers { get; set; }

    [JsonPropertyName("Volumes")]
    public VolumeData[] Volumes { get; set; }
    
    public class BuildCacheData
    {
        [JsonPropertyName("Size")]
        public long Size { get; set; }
        
        [JsonPropertyName("InUse")]
        public bool InUse { get; set; }
    }
    
    public class ContainerData
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("SizeRw")]
        public long SizeRw { get; set; }

        [JsonPropertyName("SizeRootFs")]
        public long SizeRootFs { get; set; }
    }
    
    public class ImageData
    {
        [JsonPropertyName("Containers")]
        public long Containers { get; set; }
        
        [JsonPropertyName("Id")]
        public string Id { get; set; }
        
        [JsonPropertyName("SharedSize")]
        public long SharedSize { get; set; }

        [JsonPropertyName("Size")]
        public long Size { get; set; }
    }
    
    public class VolumeData
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        
        [JsonPropertyName("UsageData")]
        public VolumeUsageData UsageData { get; set; }
    }

    public class VolumeUsageData
    {
        [JsonPropertyName("RefCount")]
        public long RefCount { get; set; }

        [JsonPropertyName("Size")]
        public long Size { get; set; }
    }
}