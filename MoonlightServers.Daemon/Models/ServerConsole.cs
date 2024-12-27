namespace MoonlightServers.Daemon.Models;

public class ServerConsole
{
    public event Func<string, Task> OnOutput;
    public event Func<string, Task> OnInput;

    public string[] Messages => GetMessages();
    private readonly Queue<string> MessageCache = new();
    private const int MaxMessagesInCache = 250; //TODO: Config

    public async Task WriteToOutput(string content)
    {
        lock (MessageCache)
        {
            MessageCache.Enqueue(content);

            if (MessageCache.Count > MaxMessagesInCache)
                MessageCache.Dequeue();
        }

        if (OnOutput != null)
        {
            await OnOutput
                .Invoke(content)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
    
    public async Task WriteToInput(string content)
    {
        if (OnInput != null)
            await OnInput.Invoke(content);
    }

    private string[] GetMessages()
    {
        lock (MessageCache)
            return MessageCache.ToArray();
    }
}