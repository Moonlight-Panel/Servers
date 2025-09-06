using System.Text;
using Docker.DotNet;
using MoonCore.Events;
using MoonCore.Helpers;
using MoonlightServers.Daemon.ServerSystem.Interfaces;
using MoonlightServers.Daemon.ServerSystem.Models;

namespace MoonlightServers.Daemon.ServerSystem.Docker;

public class DockerConsole : IConsole
{
    private readonly EventSource<string> StdOutEventSource = new();
    private readonly ConcurrentList<string> StdOutCache = new();
    private readonly DockerClient DockerClient;
    private readonly ServerContext Context;
    private readonly ILogger Logger;

    private MultiplexedStream? BaseStream;
    private CancellationTokenSource Cts = new();

    public DockerConsole(DockerClient dockerClient, ServerContext context)
    {
        DockerClient = dockerClient;
        Context = context;
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public async Task WriteStdInAsync(string content)
    {
        if (BaseStream == null)
        {
            Logger.LogWarning("Unable to write to stdin as no stream is connected");
            return;
        }

        var contextBuffer = Encoding.UTF8.GetBytes(content);

        await BaseStream.WriteAsync(contextBuffer, 0, contextBuffer.Length, Cts.Token);
    }

    public async Task WriteStdOutAsync(string content)
    {
        // Add output cache
        if (StdOutCache.Count > 250) // TODO: Config
            StdOutCache.RemoveRange(0, 100);

        StdOutCache.Add(content);

        // Fire event
        await StdOutEventSource.InvokeAsync(content);
    }

    public async Task AttachRuntimeAsync()
    {
        var containerName = string.Format(DockerConstants.RuntimeNameTemplate, Context.Configuration.Id);

        await AttachToContainer(containerName);
    }

    public async Task AttachInstallationAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, Context.Configuration.Id);

        await AttachToContainer(containerName);
    }

    private async Task AttachToContainer(string containerName)
    {
        // Cancels previous active read task if it exists
        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();

        // Reset cancellation token
        Cts = new();

        // Start reading task
        Task.Run(async () =>
        {
            // This loop is here to reconnect to the stream when connection is lost.
            // This can occur when docker restarts for example

            while (!Cts.IsCancellationRequested)
            {
                try
                {
                    using var stream = await DockerClient.Containers.AttachContainerAsync(
                        containerName,
                        true,
                        new()
                        {
                            Stderr = true,
                            Stdin = true,
                            Stdout = true,
                            Stream = true
                        },
                        Cts.Token
                    );

                    BaseStream = stream;

                    var buffer = new byte[1024];

                    try
                    {
                        // Read while server tasks are not canceled
                        while (!Cts.Token.IsCancellationRequested)
                        {
                            var readResult = await BaseStream.ReadOutputAsync(
                                buffer,
                                0,
                                buffer.Length,
                                Cts.Token
                            );

                            if (readResult.EOF)
                                break;

                            var decodedText = Encoding.UTF8.GetString(buffer, 0, readResult.Count);
                            
                            await WriteStdOutAsync(decodedText);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignored
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignored
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(e, "An unhandled error occured while reading from container stream");
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignored
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "An error occured while attaching to container");
                }
            }

            Logger.LogDebug("Disconnected from container stream");
        });
    }

    public async Task FetchRuntimeAsync()
    {
        var containerName = string.Format(DockerConstants.RuntimeNameTemplate, Context.Configuration.Id);

        await FetchFromContainer(containerName);
    }

    public async Task FetchInstallationAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, Context.Configuration.Id);

        await FetchFromContainer(containerName);
    }

    private async Task FetchFromContainer(string containerName)
    {
        var logStream = await DockerClient.Containers.GetContainerLogsAsync(containerName, true, new()
        {
            Follow = false,
            ShowStderr = true,
            ShowStdout = true
        });

        var combinedOutput = await logStream.ReadOutputToEndAsync(Cts.Token);
        var contentToAdd = combinedOutput.stdout + combinedOutput.stderr;

        await WriteStdOutAsync(contentToAdd);
    }

    public Task ClearCacheAsync()
    {
        StdOutCache.Clear();
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetCacheAsync()
    {
        return Task.FromResult<IEnumerable<string>>(StdOutCache);
    }

    public async Task<IAsyncDisposable> SubscribeStdOutAsync(Func<string, ValueTask> callback)
        => await StdOutEventSource.SubscribeAsync(callback);

    public async ValueTask DisposeAsync()
    {
        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();

        if (BaseStream != null)
            BaseStream.Dispose();
    }
}