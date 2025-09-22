using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Attributes;
using MoonlightServers.Daemon.Configuration;

namespace MoonlightServers.Daemon.Services;

[Singleton]
public class DockerImageService
{
    private readonly DockerClient DockerClient;
    private readonly AppConfiguration Configuration;
    private readonly ILogger<DockerImageService> Logger;

    private readonly Dictionary<string, TaskCompletionSource> PendingDownloads = new();

    public DockerImageService(
        DockerClient dockerClient,
        ILogger<DockerImageService> logger,
        AppConfiguration configuration
    )
    {
        Configuration = configuration;
        DockerClient = dockerClient;
        Logger = logger;
    }

    public async Task DownloadAsync(string name, Action<string>? onProgressUpdated = null)
    {
        // If there is already a download for this image occuring, we want to wait for this to complete instead
        // of calling docker to download it again
        if (PendingDownloads.TryGetValue(name, out var downloadTaskCompletion))
        {
            await downloadTaskCompletion.Task;
            return;
        }

        var tsc = new TaskCompletionSource();
        PendingDownloads.Add(name, tsc);

        try
        {
            // Figure out if and which credentials to use by checking for the domain
            AuthConfig credentials = new();

            var domain = GetDomainFromDockerImageName(name);

            var configuredCredentials = Configuration.Docker.Credentials.FirstOrDefault(x =>
                x.Domain.Equals(domain, StringComparison.InvariantCultureIgnoreCase)
            );

            // Apply credentials configuration if specified
            if (configuredCredentials != null)
            {
                credentials.Username = configuredCredentials.Username;
                credentials.Password = configuredCredentials.Password;
                credentials.Email = configuredCredentials.Email;
            }

            // Now we want to pull the image
            await DockerClient.Images.CreateImageAsync(new()
                {
                    FromImage = name
                },
                credentials,
                new Progress<JSONMessage>(async message =>
                {
                    if (message.Progress == null)
                        return;

                    var line = $"[{message.ID}] {message.ProgressMessage}";

                    Logger.LogDebug("{line}", line);

                    if (onProgressUpdated != null)
                        onProgressUpdated.Invoke(line);
                })
            );
            
            tsc.SetResult();
            PendingDownloads.Remove(name);
        }
        catch (Exception e)
        {
            Logger.LogError("An error occured while download image {name}: {e}", name, e);

            tsc.SetException(e);
            PendingDownloads.Remove(name);
            
            throw;
        }
    }

    private string GetDomainFromDockerImageName(string name) // Method names are my passion ;)
    {
        var nameParts = name.Split("/");

        // If it has 1 part -> just the image name (e.g., "ubuntu")
        // If it has 2 parts -> usually "user/image" (e.g., "library/ubuntu")
        // If it has 3 or more -> assume first part is the registry domain

        if (nameParts.Length >= 3 ||
            (nameParts.Length >= 2 && nameParts[0].Contains('.') || nameParts[0].Contains(':')))
            return nameParts[0]; // Registry domain is explicitly specified

        return "docker.io"; // Default Docker registry
    }
}