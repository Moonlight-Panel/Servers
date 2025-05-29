using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MoonlightServers.Daemon.Http.Hubs;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class ConsoleSubSystem : ServerSubSystem
{
    public event Func<string, Task>? OnOutput;
    public event Func<string, Task>? OnInput;

    private MultiplexedStream? Stream;
    private readonly List<string> OutputCache = new();

    private readonly IHubContext<ServerWebSocketHub> HubContext;
    private readonly DockerClient DockerClient;

    public ConsoleSubSystem(
        Server server,
        ILogger logger,
        IHubContext<ServerWebSocketHub> hubContext,
        DockerClient dockerClient
    ) : base(server, logger)
    {
        HubContext = hubContext;
        DockerClient = dockerClient;
    }

    public override Task Initialize()
    {
        OnInput += async content =>
        {
            if(Stream == null)
                return;
            
            var contentBuffer = Encoding.UTF8.GetBytes(content);
            await Stream.WriteAsync(contentBuffer, 0, contentBuffer.Length, Server.TaskCancellation);
        };
        
        return Task.CompletedTask;
    }

    public async Task Attach(string containerId)
    {
        Stream = await DockerClient.Containers.AttachContainerAsync(containerId,
            true,
            new ContainerAttachParameters()
            {
                Stderr = true,
                Stdin = true,
                Stdout = true,
                Stream = true
            },
            Server.TaskCancellation
        );

        // Reading
        Task.Run(async () =>
        {
            while (!Server.TaskCancellation.IsCancellationRequested)
            {
                var buffer = new byte[1024];

                try
                {
                    var readResult = await Stream.ReadOutputAsync(
                        buffer,
                        0,
                        buffer.Length,
                        Server.TaskCancellation
                    );

                    if (readResult.EOF)
                        break;

                    var resizedBuffer = new byte[readResult.Count];
                    Array.Copy(buffer, resizedBuffer, readResult.Count);
                    buffer = new byte[buffer.Length];

                    var decodedText = Encoding.UTF8.GetString(resizedBuffer);
                    await WriteOutput(decodedText);
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
                    Logger.LogWarning("An unhandled error occured while reading from container stream: {e}", e);
                }
            }
            
            // Reset stream so no further inputs will be piped to it
            Stream = null;
            
            Logger.LogDebug("Disconnected from container stream");
        });
    }

    public async Task WriteOutput(string output)
    {
        lock (OutputCache)
        {
            // Shrink cache if it exceeds the maximum
            if (OutputCache.Count > 400)
                OutputCache.RemoveRange(0, 100);

            OutputCache.Add(output);
        }

        if (OnOutput != null)
            await OnOutput.Invoke(output);

        await HubContext.Clients
            .Group(Configuration.Id.ToString())
            .SendAsync("ConsoleOutput", output);
    }

    public async Task WriteMoonlight(string output)
    {
        await WriteOutput($"\x1b[38;5;16;48;5;135m\x1b[39m\x1b[1m Moonlight \x1b[0m\x1b[38;5;250m\x1b[3m {output}\x1b[0m\n\r");
    }

    public async Task WriteInput(string input)
    {
        if (OnInput != null)
            await OnInput.Invoke(input);
    }

    public Task<string[]> RetrieveCache()
    {
        string[] result;

        lock (OutputCache)
            result = OutputCache.ToArray();

        return Task.FromResult(result);
    }
}