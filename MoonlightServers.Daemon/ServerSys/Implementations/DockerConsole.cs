using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using MoonCore.Helpers;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerConsole : IConsole
{
    public IObservable<string> OnOutput => OnOutputSubject;
    public IObservable<string> OnInput => OnInputSubject;

    private readonly Subject<string> OnOutputSubject = new();
    private readonly Subject<string> OnInputSubject = new();

    private readonly ConcurrentList<string> OutputCache = new();
    private readonly DockerClient DockerClient;
    private readonly ILogger<DockerConsole> Logger;
    private readonly ServerContext Context;

    private MultiplexedStream? CurrentStream;
    private CancellationTokenSource Cts = new();

    public DockerConsole(
        ServerContext context,
        DockerClient dockerClient,
        ILogger<DockerConsole> logger
    )
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

    private Task AttachStream(string containerName)
    {
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

        return Task.CompletedTask;
    }

    public Task WriteToOutput(string content)
    {
        OutputCache.Add(content);

        if (OutputCache.Count > 250) // TODO: Config
        {
            // TODO: Replace with remove range once it becomes available in mooncore
            for (var i = 0; i < 100; i++)
            {
                OutputCache.RemoveAt(i);
            }
        }

        OnOutputSubject.OnNext(content);
        return Task.CompletedTask;
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
    }

    public async Task WriteToMoonlight(string content)
        => await WriteToOutput(
            $"\x1b[0;38;2;255;255;255;48;2;124;28;230m Moonlight \x1b[0m\x1b[38;5;250m\x1b[3m {content}\x1b[0m\n\r");

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