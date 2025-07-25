using System.Reactive.Linq;
using System.Reactive.Subjects;
using Docker.DotNet;
using MoonCore.Helpers;
using MoonlightServers.Daemon.ServerSys.Abstractions;

namespace MoonlightServers.Daemon.ServerSys.Implementations;

public class DockerConsole : IConsole
{
    public IAsyncObservable<string> OnOutput => OnOutputSubject.ToAsyncObservable();
    public IAsyncObservable<string> OnInput => OnInputSubject.ToAsyncObservable();

    private readonly AsyncSubject<string> OnOutputSubject = new();
    private readonly AsyncSubject<string> OnInputSubject = new();

    private readonly ConcurrentList<string> OutputCache = new();

    private readonly DockerClient DockerClient;
    private readonly ILogger<DockerConsole> Logger;
    private MultiplexedStream? CurrentStream;
    private CancellationTokenSource Cts = new();

    public Task Initialize()
        => Task.CompletedTask;

    public Task Sync()
        => Task.CompletedTask;

    public async Task AttachToRuntime()
    {
        throw new NotImplementedException();
    }

    public async Task AttachToInstallation()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

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
        
        if(CurrentStream != null)
            CurrentStream.Dispose();
    }
}