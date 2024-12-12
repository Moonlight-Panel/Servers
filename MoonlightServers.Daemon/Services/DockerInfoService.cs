using Docker.DotNet;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Helpers;
using MoonlightServers.Daemon.Models.UnsafeDocker;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class DockerInfoService
{
    private readonly DockerClient DockerClient;
    private readonly UnsafeDockerClient UnsafeDockerClient;

    public DockerInfoService(DockerClient dockerClient, UnsafeDockerClient unsafeDockerClient)
    {
        DockerClient = dockerClient;
        UnsafeDockerClient = unsafeDockerClient;
    }

    public async Task<string> GetDockerVersion()
    {
        var version = await DockerClient.System.GetVersionAsync();

        return $"{version.Version} commit {version.GitCommit} ({version.APIVersion})";
    }

    public async Task<UsageDataReport> GetDataUsage()
    {
        var response = await UnsafeDockerClient.GetDataUsage();

        var report = new UsageDataReport()
        {
            Containers = new UsageData()
            {
                Used = response.Containers.Sum(x => x.SizeRw),
                Reclaimable = 0
            },
            Images = new UsageData()
            {
                Used = response.Images.Sum(x => x.Size),
                Reclaimable = response.Images.Where(x => x.Containers == 0).Sum(x => x.Size)
            },
            BuildCache = new UsageData()
            {
                Used = response.BuildCache.Sum(x => x.Size),
                Reclaimable = response.BuildCache.Where(x => !x.InUse).Sum(x => x.Size)
            }
        };

        return report;
    }
}