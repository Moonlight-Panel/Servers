using MoonCore.Blazor.FlyonUi.Files;
using MoonCore.Blazor.FlyonUi.Files.Manager;
using MoonlightServers.Frontend.Services;

namespace MoonlightServers.Frontend.Helpers;

public class ServerFsAccess : IFsAccess
{
    private readonly int Id;
    private readonly ServerFileSystemService FileSystemService;

    public ServerFsAccess(int id, ServerFileSystemService fileSystemService)
    {
        Id = id;
        FileSystemService = fileSystemService;
    }

    public Task CreateFile(string path)
        => FileSystemService.Touch(Id, path);

    public Task CreateDirectory(string path)
        => FileSystemService.Mkdir(Id, path);

    public async Task<FsEntry[]> List(string path)
    {
        var entries = await FileSystemService.List(Id, path);
        
        return entries.Select(x => new FsEntry()
        {
            Name = x.Name,
            Size = x.Size,
            CreatedAt = x.CreatedAt,
            IsFolder = x.IsFolder,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }

    public Task Move(string oldPath, string newPath)
        =>  FileSystemService.Move(Id, oldPath, newPath);

    public Task Read(string path, Func<Stream, Task> onHandleData)
    {
        throw new NotImplementedException();
    }

    public Task Write(string path, Stream dataStream)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string path)
     => FileSystemService.Delete(Id, path);

    public Task UploadChunk(string path, int chunkId, long chunkSize, long totalSize, byte[] data)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> DownloadChunk(string path, int chunkId, long chunkSize)
    {
        throw new NotImplementedException();
    }
}