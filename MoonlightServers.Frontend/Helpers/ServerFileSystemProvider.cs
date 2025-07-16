/*
using System.IO.Enumeration;
using MoonCore.Blazor.FlyonUi.Helpers;
using MoonCore.Helpers;
using MoonlightServers.Frontend.Services;

namespace MoonlightServers.Frontend.Helpers;

public class ServerFileSystemProvider : IFileSystemProvider, ICompressFileSystemProvider
{
    private readonly DownloadService DownloadService;
    private readonly ServerFileSystemService FileSystemService;

    public CompressType[] CompressTypes { get; } =
    [
        new()
        {
            Extension = "zip",
            DisplayName = "ZIP Archive"
        },
        new()
        {
            Extension = "tar.gz",
            DisplayName = "GZ Compressed Tar Archive"
        }
    ];

    private readonly int ServerId;

    public ServerFileSystemProvider(
        int serverId,
        ServerFileSystemService fileSystemService,
        DownloadService downloadService
    )
    {
        ServerId = serverId;
        FileSystemService = fileSystemService;
        DownloadService = downloadService;
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
        await Upload(_ => Task.CompletedTask, path, stream);
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
        var downloadSession = await FileSystemService.Download(ServerId, path);

        using var httpClient = new HttpClient();
        return await httpClient.GetStreamAsync(downloadSession.DownloadUrl);
    }

    public async Task Download(Func<int, Task> updateProgress, string path, string fileName)
    {
        var downloadSession = await FileSystemService.Download(ServerId, path);

        await DownloadService.DownloadUrl(fileName, downloadSession.DownloadUrl,
            async (loaded, total) =>
            {
                var percent = total == 0 ? 0 : (int)Math.Round((float)loaded / total * 100);
                await updateProgress.Invoke(percent);
            }
        );
    }

    public async Task Upload(Func<int, Task> updateProgress, string path, Stream stream)
    {
        using var httpClient = new HttpClient();
        
        var uploadSession = await FileSystemService.Upload(ServerId);
        
        var size = stream.Length;
        var chunkSize = ByteConverter.FromMegaBytes(20).Bytes;

        var chunks = size / chunkSize;
        chunks += size % chunkSize > 0 ? 1 : 0;
        
        for (var chunkId = 0; chunkId < chunks; chunkId++)
        {
            var percent = (int)Math.Round((chunkId + 1f) / chunks * 100);
            await updateProgress.Invoke(percent);
            
            var buffer = new byte[chunkSize];
            var bytesRead = await stream.ReadAsync(buffer);

            var uploadForm = new MultipartFormDataContent();
            uploadForm.Add(new ByteArrayContent(buffer, 0, bytesRead), "file", "file");
            
            await httpClient.PostAsync(
                $"{uploadSession.UploadUrl}&totalSize={size}&chunkId={chunkId}&path={path}",
                uploadForm
            );
        }
    }

    public async Task Compress(CompressType type, string path, string[] itemsToCompress)
    {
        await FileSystemService.Compress(
            ServerId,
            type.Extension.Replace(".", ""),
            itemsToCompress,
            path
        );
    }

    public async Task Decompress(CompressType type, string path, string destination)
    {
        await FileSystemService.Decompress(
            ServerId,
            type.Extension.Replace(".", ""),
            path,
            destination
        );
    }
}*/