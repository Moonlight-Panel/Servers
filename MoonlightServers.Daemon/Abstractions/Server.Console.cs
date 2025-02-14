using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MoonlightServers.Daemon.Abstractions;

public partial class Server
{
    private async Task AttachConsole(string containerId)
    {
        var dockerClient = ServiceProvider.GetRequiredService<DockerClient>();

        var stream = await dockerClient.Containers.AttachContainerAsync(containerId, true,
            new ContainerAttachParameters()
            {
                Stderr = true,
                Stdin = true,
                Stdout = true,
                Stream = true
            },
            Cancellation.Token
        );

        // Reading
        Task.Run(async () =>
        {
            while (!Cancellation.Token.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];

                    var readResult = await stream.ReadOutputAsync(
                        buffer,
                        0,
                        buffer.Length,
                        Cancellation.Token
                    );

                    if (readResult.EOF)
                        break;

                    var resizedBuffer = new byte[readResult.Count];
                    Array.Copy(buffer, resizedBuffer, readResult.Count);
                    buffer = new byte[buffer.Length];

                    var decodedText = Encoding.UTF8.GetString(resizedBuffer);
                    await Console.WriteToOutput(decodedText);
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
        });

        // Writing
        Console.OnInput += async content =>
        {
            var contentBuffer = Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(contentBuffer, 0, contentBuffer.Length, Cancellation.Token);
        };
    }

    private async Task LogToConsole(string message)
    {
        await Console.WriteToOutput($"\x1b[38;5;16;48;5;135m\x1b[39m\x1b[1m Moonlight \x1b[0m\x1b[38;5;250m\x1b[3m {message}\x1b[0m\n\r");
    }

    public Task<string[]> GetConsoleMessages()
        => Task.FromResult(Console.Messages);
}