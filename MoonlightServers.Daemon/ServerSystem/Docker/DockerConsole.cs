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

    private MultiplexedStream? CurrentStream;
    private CancellationTokenSource Cts = new();

    public DockerConsole(DockerClient dockerClient, ServerContext context)
    {
        DockerClient = dockerClient;
        Context = context;
        Logger = Context.Logger;
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public async Task WriteStdInAsync(string content)
    {
        if (CurrentStream == null)
        {
            Logger.LogWarning("Unable to write to stdin as no stream is connected");
            return;
        }

        var contextBuffer = Encoding.UTF8.GetBytes(content);

        await CurrentStream.WriteAsync(contextBuffer, 0, contextBuffer.Length, Cts.Token);
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

        await AttachToContainerAsync(containerName);
    }

    public async Task AttachInstallationAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, Context.Configuration.Id);

        await AttachToContainerAsync(containerName);
    }

    private async Task AttachToContainerAsync(string containerName)
    {
        var cts = new CancellationTokenSource();
        
        // Cancels previous active read task if it exists
        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();

        // Update the current cancellation token
        Cts = cts;

        // Start reading task
        Task.Run(async () =>
        {
            // This loop is here to reconnect to the stream when connection is lost.
            // This can occur when docker restarts for example

            while (!cts.IsCancellationRequested)
            {
                MultiplexedStream? innerStream = null;
                
                try
                {
                    Logger.LogTrace("Attaching");
                    
                    innerStream = await DockerClient.Containers.AttachContainerAsync(
                        containerName,
                        true,
                        new()
                        {
                            Stderr = true,
                            Stdin = true,
                            Stdout = true,
                            Stream = true
                        },
                        cts.Token
                    );

                    CurrentStream = innerStream;

                    var buffer = new byte[1024];

                    try
                    {
                        // Read while server tasks are not canceled
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var readResult = await innerStream.ReadOutputAsync(
                                buffer,
                                0,
                                buffer.Length,
                                cts.Token
                            );

                            if (readResult.EOF)
                                await cts.CancelAsync();

                            var decodedText = Encoding.UTF8.GetString(buffer, 0, readResult.Count);

                            await WriteStdOutAsync(decodedText);
                        }
                        
                        Logger.LogTrace("Read loop exited");
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
                catch (DockerContainerNotFoundException)
                {
                    // Container got removed. Stop the reconnect loop
                    
                    Logger.LogDebug("Container '{name}' got removed. Stopping reconnect stream for console", containerName);
                    await cts.CancelAsync();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "An error occured while attaching to container");
                }
                
                innerStream?.Dispose();
            }

            Logger.LogDebug("Disconnected from container stream");
        });
    }

    public async Task FetchRuntimeAsync()
    {
        var containerName = string.Format(DockerConstants.RuntimeNameTemplate, Context.Configuration.Id);

        await FetchFromContainerAsync(containerName);
    }

    public async Task FetchInstallationAsync()
    {
        var containerName = string.Format(DockerConstants.InstallationNameTemplate, Context.Configuration.Id);

        await FetchFromContainerAsync(containerName);
    }

    private async Task FetchFromContainerAsync(string containerName)
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

        if (CurrentStream != null)
            CurrentStream.Dispose();
    }
}