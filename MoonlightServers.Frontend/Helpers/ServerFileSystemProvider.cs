using MoonCore.Blazor.Tailwind.Fm;
using MoonlightServers.Frontend.Services;

namespace MoonlightServers.Frontend.Helpers;

public class ServerFileSystemProvider : IFileSystemProvider
{
    private readonly int ServerId;
    private readonly ServerFileSystemService FileSystemService;

    public ServerFileSystemProvider(
        int serverId,
        ServerFileSystemService fileSystemService
    )
    {
        ServerId = serverId;
        FileSystemService = fileSystemService;
    }

    public async Task<FileSystemEntry[]> List(string path)
    {
        var result = await FileSystemService.List(ServerId, path);

        return result
            .Select(x => new FileSystemEntry()
            {
                Name = x.Name,
                Size = x.Size,
                IsFile = x.IsFile,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToArray();
    }

    public async Task Create(string path, Stream stream)
    {
        await FileSystemService.Upload(ServerId, path, stream);
    }

    public async Task Move(string oldPath, string newPath)
    {
        await FileSystemService.Move(ServerId, oldPath, newPath);
    }

    public async Task Delete(string path)
    {
        await FileSystemService.Delete(ServerId, path);
    }

    public async Task CreateDirectory(string path)
    {
        await FileSystemService.Mkdir(ServerId, path);
    }

    public async Task<Stream> Read(string path)
    {
        return await FileSystemService.Download(ServerId, path);
    }
}