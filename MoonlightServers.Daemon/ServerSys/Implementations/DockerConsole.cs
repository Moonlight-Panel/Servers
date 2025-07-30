using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using MoonCore.Helpers;
using MoonCore.Observability;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerConsole : IConsole
{
    public IAsyncObservable<string> OnOutput => OnOutputSubject;
    public IAsyncObservable<string> OnInput => OnInputSubject;

    private readonly EventSubject<string> OnOutputSubject = new();
    private readonly EventSubject<string> OnInputSubject = new();

    private readonly ConcurrentList<string> OutputCache = new();
    private readonly DockerClient DockerClient;
    private readonly ILogger<DockerConsole> Logger;
    private readonly ServerContext Context;

    private MultiplexedStream? CurrentStream;
    private CancellationTokenSource Cts = new();

    private const string MlPrefix = "\x1b[1;38;2;200;90;200mM\x1b[1;38;2;204;110;230mo\x1b[1;38;2;170;130;245mo\x1b[1;38;2;140;150;255mn\x1b[1;38;2;110;180;255ml\x1b[1;38;2;100;200;255mi\x1b[1;38;2;100;220;255mg\x1b[1;38;2;120;235;255mh\x1b[1;38;2;140;250;255mt\x1b[0m \x1b[3;38;2;200;200;200m{0}\x1b[0m\n\r";

    public DockerConsole(
        ServerContext context,
        DockerClient dockerClient,
        ILogger<DockerConsole> logger)
    {
        Context = context;
        DockerClient = dockerClient;
        Logger = logger;
    }

    public Task Initialize()
        => Task.CompletedTask;

    public Task Sync()
        => Task.CompletedTask;

    public async Task AttachToRuntime()
    {
        var containerName = $"moonlight-runtime-{Context.Configuration.Id}";
        await AttachStream(containerName);
    }

    public async Task AttachToInstallation()
    {
        var containerName = $"moonlight-install-{Context.Configuration.Id}";
        await AttachStream(containerName);
    }

    public async Task Detach()
    {
        Logger.LogDebug("Detaching stream");

        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();
    }

    public async Task CollectFromRuntime()
        => await CollectFromContainer($"moonlight-runtime-{Context.Configuration.Id}");

    public async Task CollectFromInstallation()
        => await CollectFromContainer($"moonlight-install-{Context.Configuration.Id}");

    private async Task CollectFromContainer(string containerName)
    {
        var logStream = await DockerClient.Containers.GetContainerLogsAsync(containerName, true, new()
        {
            Follow = false,
            ShowStderr = true,
            ShowStdout = true
        });

        var combinedOutput = await logStream.ReadOutputToEndAsync(CancellationToken.None);
        var contentToAdd = combinedOutput.stdout + combinedOutput.stderr;

        await WriteToOutput(contentToAdd);
    }

    private async Task AttachStream(string containerName)
    {
        // This stops any previously existing stream reading if
        // any is currently running
        if (!Cts.IsCancellationRequested)
            await Cts.CancelAsync();

        // Reset
        Cts = new();

        Task.Run(async () =>
        {
            // This loop is here to reconnect to the container if for some reason the container
            // attach stream fails before the server tasks have been canceled i.e. the before the server
            // goes offline

            while (!Cts.Token.IsCancellationRequested)
            {
                try
                {
                    CurrentStream = await DockerClient.Containers.AttachContainerAsync(
                        containerName,
                        true,
                        new ContainerAttachParameters()
                        {
                            Stderr = true,
                            Stdin = true,
                            Stdout = true,
                            Stream = true
                        },
                        Cts.Token
                    );

                    var buffer = new byte[1024];

                    try
                    {
                        // Read while server tasks are not canceled
                        while (!Cts.Token.IsCancellationRequested)
                        {
                            var readResult = await CurrentStream.ReadOutputAsync(
                                buffer,
                                0,
                                buffer.Length,
                                Cts.Token
                            );

                            if (readResult.EOF)
                                break;

                            var resizedBuffer = new byte[readResult.Count];
                            Array.Copy(buffer, resizedBuffer, readResult.Count);
                            buffer = new byte[buffer.Length];

                            var decodedText = Encoding.UTF8.GetString(resizedBuffer);
                            await WriteToOutput(decodedText);
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
                    finally
                    {
                        CurrentStream.Dispose();
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


            // Reset stream so no further inputs will be piped to it
            CurrentStream = null;

            Logger.LogDebug("Disconnected from container stream");
        }, Cts.Token);
    }

    public async Task WriteToOutput(string content)
    {
        OutputCache.Add(content);

        if (OutputCache.Count > 250) // TODO: Config
            OutputCache.RemoveRange(0, 100);

        await OnOutputSubject.OnNextAsync(content);
    }

    public async Task WriteToInput(string content)
    {
        if (CurrentStream == null)
            return;

        var contentBuffer = Encoding.UTF8.GetBytes(content);

        await CurrentStream.WriteAsync(
            contentBuffer,
            0,
            contentBuffer.Length,
            Cts.Token
        );

        await OnInputSubject.OnNextAsync(content);
    }

    public async Task WriteToMoonlight(string content)
        => await WriteToOutput(string.Format(MlPrefix, content));

    public Task ClearOutput()
    {
        OutputCache.Clear();
        return Task.CompletedTask;
    }

    public string[] GetOutput()
        => OutputCache.ToArray();

    public async ValueTask DisposeAsync()
    {
        if (!Cts.IsCancellationRequested)
        {
            await Cts.CancelAsync();
            Cts.Dispose();
        }

        if (CurrentStream != null)
            CurrentStream.Dispose();
    }
}