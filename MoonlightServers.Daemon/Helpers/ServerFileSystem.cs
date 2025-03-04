using Mono.Unix.Native;
using MoonCore.Unix.SecureFs;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;

namespace MoonlightServers.Daemon.Helpers;

public class ServerFileSystem
{
    private readonly SecureFileSystem FileSystem;

    public ServerFileSystem(SecureFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public Task<ServerFileSystemResponse[]> List(string inputPath)
    {
        var path = Normalize(inputPath);
        var entries = FileSystem.ReadDir(path);

        var result = entries
            .Select(x => new ServerFileSystemResponse()
            {
                Name = x.Name,
                IsFile = x.IsFile,
                Size = x.Size,
                UpdatedAt = x.LastChanged,
                CreatedAt = x.CreatedAt
            })
            .ToArray();

        return Task.FromResult(result);
    }

    public Task Move(string inputOldPath, string inputNewPath)
    {
        var oldPath = Normalize(inputOldPath);
        var newPath = Normalize(inputNewPath);
        
        FileSystem.Rename(oldPath, newPath);
        
        return Task.CompletedTask;
    }

    public Task Delete(string inputPath)
    {
        var path = Normalize(inputPath);
        
        FileSystem.RemoveAll(path);
        
        return Task.CompletedTask;
    }

    public Task Mkdir(string inputPath)
    {
        var path = Normalize(inputPath);
        
        FileSystem.MkdirAll(path, FilePermissions.ACCESSPERMS);
        
        return Task.CompletedTask;
    }

    public Task Create(string inputPath, Stream dataStream)
    {
        var path = Normalize(inputPath);
        
        var parentDirectory = Path.GetDirectoryName(path);
        
        if(!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/")
            FileSystem.MkdirAll(parentDirectory, FilePermissions.ACCESSPERMS);
        
        FileSystem.WriteFile(path, dataStream);

        return Task.CompletedTask;
    }
    
    public Task Read(string inputPath, Func<Stream, Task> onHandle)
    {
        var path = Normalize(inputPath);
        
        FileSystem.OpenFile(path, stream =>
        {
            // No try catch here because the safe fs abstraction already handles every error occuring in the handle
            onHandle.Invoke(stream).Wait();
        });

        return Task.CompletedTask;
    }

    private string Normalize(string path)
    {
        return path
            .Replace("//", "/")
            .Replace("..", "")
            .TrimStart('/');
    }
}