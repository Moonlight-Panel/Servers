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

    public Task CreateFileAsync(string path)
        => FileSystemService.TouchAsync(Id, path);

    public Task CreateDirectoryAsync(string path)
        => FileSystemService.MkdirAsync(Id, path);

    public async Task<FsEntry[]> ListAsync(string path)
    {
        var entries = await FileSystemService.ListAsync(Id, path);
        
        return entries.Select(x => new FsEntry()
        {
            Name = x.Name,
            Size = x.Size,
            CreatedAt = x.CreatedAt,
            IsFolder = x.IsFolder,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }

    public Task MoveAsync(string oldPath, string newPath)
        =>  FileSystemService.MoveAsync(Id, oldPath, newPath);

    public Task ReadAsync(string path, Func<Stream, Task> onHandleData)
    {
        throw new NotImplementedException();
    }

    public Task WriteAsync(string path, Stream dataStream)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string path)
     => FileSystemService.DeleteAsync(Id, path);

    public Task UploadChunkAsync(string path, int chunkId, long chunkSize, long totalSize, byte[] data)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> DownloadChunkAsync(string path, int chunkId, long chunkSize)
    {
        throw new NotImplementedException();
    }
}