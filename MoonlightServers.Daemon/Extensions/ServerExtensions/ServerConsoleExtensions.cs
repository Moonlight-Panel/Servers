using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using MoonlightServers.Daemon.Models;

namespace MoonlightServers.Daemon.Extensions.ServerExtensions;

public static class ServerConsoleExtensions
{
    public static async Task Attach(this Server server)
    {
        var dockerClient = server.ServiceProvider.GetRequiredService<DockerClient>();

        var stream = await dockerClient.Containers.AttachContainerAsync(server.ContainerId, true,
            new ContainerAttachParameters()
            {
                Stderr = true,
                Stdin = true,
                Stdout = true,
                Stream = true
            },
            server.Cancellation.Token
        );

        Task.Run(async () =>
        {
            while (!server.Cancellation.Token.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];

                    var readResult = await stream.ReadOutputAsync(
                        buffer,
                        0,
                        buffer.Length,
                        server.Cancellation.Token
                    );
                    
                    if(readResult.EOF)
                        break;

                    var resizedBuffer = new byte[readResult.Count];
                    Array.Copy(buffer, resizedBuffer, readResult.Count);
                    buffer = new byte[buffer.Length];

                    var decodedText = Encoding.UTF8.GetString(resizedBuffer);
                    await server.Console.WriteToOutput(decodedText);
                }
                catch (TaskCanceledException)
                {
                    // Ignored
                }
                catch (Exception e)
                {
                    server.Logger.LogWarning("An unhandled error occured while reading from container stream: {e}", e);
                }
            }
        });
    }
}