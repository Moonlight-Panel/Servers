using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Unix.Native;
using MoonCore.Unix.Exceptions;
using MoonCore.Unix.SecureFs;
using MoonlightServers.DaemonShared.DaemonSide.Http.Responses.Servers;
using MoonlightServers.DaemonShared.Enums;

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

    public Task CreateChunk(string inputPath, long totalSize, long positionToSkip, Stream chunkStream)
    {
        var path = Normalize(inputPath);

        var parentDirectory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/")
            FileSystem.MkdirAll(parentDirectory, FilePermissions.ACCESSPERMS);
        
        FileSystem.OpenFileWrite(path, fileStream =>
        {
            if (fileStream.Length != totalSize)
                fileStream.SetLength(totalSize);

            fileStream.Position = positionToSkip;
            
            chunkStream.CopyTo(fileStream);
            fileStream.Flush();
        }, OpenFlags.O_CREAT | OpenFlags.O_RDWR); // We use these custom flags to ensure we aren't overwriting the file

        return Task.CompletedTask;
    }

    public Task Create(string inputPath, Stream dataStream)
    {
        var path = Normalize(inputPath);

        var parentDirectory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/")
            FileSystem.MkdirAll(parentDirectory, FilePermissions.ACCESSPERMS);

        FileSystem.OpenFileWrite(path, stream =>
        {
            stream.Position = 0;
            dataStream.CopyTo(stream);

            stream.Flush();
        });

        return Task.CompletedTask;
    }

    public Task Read(string inputPath, Func<Stream, Task> onHandle)
    {
        var path = Normalize(inputPath);

        FileSystem.OpenFileRead(path, stream =>
        {
            // No try catch here because the safe fs abstraction already handles every error occuring in the handle
            onHandle.Invoke(stream).Wait();
        });

        return Task.CompletedTask;
    }

    #region Compression

    public Task Compress(string[] itemsInput, string destinationInput, CompressType type)
    {
        var destination = Normalize(destinationInput);
        var items = itemsInput.Select(Normalize);

        if (type == CompressType.Zip)
        {
            FileSystem.OpenFileWrite(destination, stream =>
            {
                using var zipStream = new ZipOutputStream(stream);

                foreach (var item in items)
                    AddItemToZip(item, zipStream);

                zipStream.Flush();
                stream.Flush();

                zipStream.Close();
            });
        }
        else if (type == CompressType.TarGz)
        {
            FileSystem.OpenFileWrite(destination, stream =>
            {
                using var gzStream = new GZipOutputStream(stream);
                using var tarStream = new TarOutputStream(gzStream, Encoding.UTF8);

                foreach (var item in items)
                    AddItemToTar(item, tarStream);

                tarStream.Flush();
                gzStream.Flush();
                stream.Flush();

                tarStream.Close();
                gzStream.Close();
            });
        }

        return Task.CompletedTask;
    }

    public Task Decompress(string pathInput, string destinationInput, CompressType type)
    {
        var path = Normalize(pathInput);
        var destination = Normalize(destinationInput);

        if (type == CompressType.Zip)
        {
            FileSystem.OpenFileRead(path, fileStream =>
            {
                var zipInputStream = new ZipInputStream(fileStream);
                
                ExtractZip(zipInputStream, destination);
            });
        }
        else if (type == CompressType.TarGz)
        {
            FileSystem.OpenFileRead(path, fileStream =>
            {
                var gzInputStream = new GZipInputStream(fileStream);
                var zipInputStream = new TarInputStream(gzInputStream, Encoding.UTF8);
                
                ExtractTar(zipInputStream, destination);
            });
        }
        
        return Task.CompletedTask;
    }

    private void AddItemToZip(string path, ZipOutputStream outputStream)
    {
        var item = FileSystem.Stat(path);

        if (item.IsDirectory)
        {
            var contents = FileSystem.ReadDir(path);

            foreach (var content in contents)
            {
                AddItemToZip(
                    Path.Combine(path, content.Name),
                    outputStream
                );
            }
        }
        else
        {
            var entry = new ZipEntry(path)
            {
                Size = item.Size,
                DateTime = item.LastChanged
            };

            outputStream.PutNextEntry(entry);

            FileSystem.OpenFileRead(path, stream => { stream.CopyTo(outputStream); });

            outputStream.CloseEntry();
        }
    }

    private void AddItemToTar(string path, TarOutputStream outputStream)
    {
        var item = FileSystem.Stat(path);

        if (item.IsDirectory)
        {
            var contents = FileSystem.ReadDir(path);

            foreach (var content in contents)
            {
                AddItemToTar(
                    Path.Combine(path, content.Name),
                    outputStream
                );
            }
        }
        else
        {
            var entry = TarEntry.CreateTarEntry(path);

            entry.Name = path;
            entry.Size = item.Size;
            entry.ModTime = item.LastChanged;

            outputStream.PutNextEntry(entry);

            FileSystem.OpenFileRead(path, stream =>
            {
                stream.CopyTo(outputStream);
            });

            outputStream.CloseEntry();
        }
    }

    private void ExtractZip(ZipInputStream inputStream, string destination)
    {
        while (true)
        {
            var entry = inputStream.GetNextEntry();
            
            if(entry == null || entry.IsDirectory)
                break;

            var fileDestination = Path.Combine(destination, entry.Name);
            
            var parentDirectory = Path.GetDirectoryName(fileDestination);

            if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/")
                FileSystem.MkdirAll(parentDirectory, FilePermissions.ACCESSPERMS);
            
            FileSystem.OpenFileWrite(fileDestination, stream =>
            {
                stream.Position = 0;
                
                inputStream.CopyTo(stream);

                stream.Flush();
            }); // This will override the file if it exists
        }
    }
    
    private void ExtractTar(TarInputStream inputStream, string destination)
    {
        while (true)
        {
            var entry = inputStream.GetNextEntry();
            
            if(entry == null || entry.IsDirectory)
                break;

            var fileDestination = Path.Combine(destination, entry.Name);
            
            var parentDirectory = Path.GetDirectoryName(fileDestination);

            if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/")
                FileSystem.MkdirAll(parentDirectory, FilePermissions.ACCESSPERMS);
            
            FileSystem.OpenFileWrite(fileDestination, stream =>
            {
                stream.Position = 0;
                
                inputStream.CopyTo(stream);

                stream.Flush();
            }); // This will override the file if it exists
        }
    }

    #endregion

    private string Normalize(string path)
    {
        return path
            .Replace("//", "/")
            .Replace("..", "")
            .TrimStart('/');
    }
}