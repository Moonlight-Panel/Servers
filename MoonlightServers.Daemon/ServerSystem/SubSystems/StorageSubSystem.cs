using System.Diagnostics;
using Mono.Unix.Native;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonCore.Unix.SecureFs;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class StorageSubSystem : ServerSubSystem
{
    private readonly AppConfiguration AppConfiguration;
    private SecureFileSystem? SecureFileSystem;
    private ServerFileSystem ServerFileSystem;
    private ConsoleSubSystem ConsoleSubSystem;

    public string RuntimeVolumePath { get; private set; }
    public string InstallVolumePath { get; private set; }
    public string VirtualDiskPath { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsVirtualDiskMounted { get; private set; }

    public StorageSubSystem(
        Server server,
        ILogger logger,
        AppConfiguration appConfiguration
    ) : base(server, logger)
    {
        AppConfiguration = appConfiguration;

        // Runtime Volume
        var runtimePath = Path.Combine(AppConfiguration.Storage.Volumes, Configuration.Id.ToString());

        if (!runtimePath.StartsWith('/'))
            runtimePath = Path.Combine(Directory.GetCurrentDirectory(), runtimePath);

        RuntimeVolumePath = runtimePath;

        // Install Volume
        var installPath = Path.Combine(AppConfiguration.Storage.Install, Configuration.Id.ToString());

        if (!installPath.StartsWith('/'))
            installPath = Path.Combine(Directory.GetCurrentDirectory(), installPath);

        InstallVolumePath = installPath;

        // Virtual Disk
        if (!Configuration.UseVirtualDisk)
            return;

        var virtualDiskPath = Path.Combine(AppConfiguration.Storage.VirtualDisks, $"{Configuration.Id}.img");

        if (!virtualDiskPath.StartsWith('/'))
            virtualDiskPath = Path.Combine(Directory.GetCurrentDirectory(), virtualDiskPath);

        VirtualDiskPath = virtualDiskPath;
    }

    public override Task Initialize()
    {
        Logger.LogDebug("Lazy initializing server file system");

        ConsoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

        Task.Run(async () =>
        {
            try
            {
                await EnsureRuntimeVolume();

                // If we don't use a virtual disk the EnsureRuntimeVolume() method is
                // all we need in order to serve access to the file system
                if (!Configuration.UseVirtualDisk)
                    await CreateFileSystemAccessor();

                IsInitialized = true;
            }
            catch (Exception e)
            {
                var consoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

                await consoleSubSystem.WriteMoonlight(
                    "Unable to initialize server file system. Please contact the administrator");

                Logger.LogError("An unhandled error occured while lazy initializing server file system: {e}", e);
            }
        });

        return Task.CompletedTask;
    }

    public override async Task Delete()
    {
        await DeleteInstallVolume();
        await DeleteRuntimeVolume();
    }

    #region Runtime

    public async Task<ServerFileSystem> GetFileSystem()
    {
        if (!await RequestRuntimeVolume(skipPermissions: true))
            throw new HttpApiException("The file system is still initializing. Please try again later", 503);

        return ServerFileSystem;
    }

    // This method allows other sub systems to request access to the runtime volume.
    // The return value specifies if the request to the runtime volume is possible or not
    public async Task<bool> RequestRuntimeVolume(bool skipPermissions = false)
    {
        if (!IsInitialized)
            return false;

        if (!Configuration.UseVirtualDisk)
            return true; // This is the default return for all servers without a virtual disk which has been initialized
        
        // If the disk isn't already mounted, we need to mount it now
        if (!IsVirtualDiskMounted)
        {
            await MountVirtualDisk();
            
            // And in order to serve the file system we need to create the accessor for it
            await CreateFileSystemAccessor();
        }

        if(!skipPermissions)
            await EnsureRuntimePermissions();

        return IsVirtualDiskMounted;
    }

    private async Task EnsureRuntimeVolume()
    {
        if (!Directory.Exists(RuntimeVolumePath))
            Directory.CreateDirectory(RuntimeVolumePath);

        if (Configuration.UseVirtualDisk)
            await EnsureVirtualDiskCreated();
    }

    private async Task DeleteRuntimeVolume()
    {
        if (!Directory.Exists(RuntimeVolumePath))
            return;

        if (Configuration.UseVirtualDisk)
        {
            if (IsVirtualDiskMounted)
            {
                // Ensure the secure file system is no longer open
                if(SecureFileSystem != null && !SecureFileSystem.IsDisposed)
                    SecureFileSystem.Dispose();
                
                await UnmountVirtualDisk();
            }
            
            File.Delete(VirtualDiskPath);
        }
        else
        {
            if (SecureFileSystem == null) // If we are not already initialized, we are initializing now just the part we need
                SecureFileSystem = new SecureFileSystem(RuntimeVolumePath);

            foreach (var entry in SecureFileSystem.ReadDir("/"))
            {
                if(entry.IsFile)
                    SecureFileSystem.Remove(entry.Name);
                else
                    SecureFileSystem.RemoveAll(entry.Name);
            }
            
            SecureFileSystem.Dispose();
        }

        Directory.Delete(RuntimeVolumePath, true);
    }

    private Task EnsureRuntimePermissions()
    {
        ArgumentNullException.ThrowIfNull(SecureFileSystem);
        
        //TODO: Config
        var uid = (int)Syscall.getuid();
        var gid = (int)Syscall.getgid();

        if (uid == 0)
        {
            uid = 998;
            gid = 998;
        }

        foreach (var entry in SecureFileSystem.ReadDir("/"))
        {
            if (entry.IsFile)
                SecureFileSystem.Chown(entry.Name, uid, gid);
            else
                SecureFileSystem.ChownAll(entry.Name, uid, gid);
        }

        Syscall.chown(RuntimeVolumePath, uid, gid);

        return Task.CompletedTask;
    }

    private Task CreateFileSystemAccessor()
    {
        SecureFileSystem = new(RuntimeVolumePath);
        ServerFileSystem = new(SecureFileSystem);
        
        return Task.CompletedTask;
    }

    #endregion

    #region Installation

    public Task EnsureInstallVolume()
    {
        if (!Directory.Exists(InstallVolumePath))
            Directory.CreateDirectory(InstallVolumePath);
        
        return Task.CompletedTask;
    }

    public Task DeleteInstallVolume()
    {
        if (!Directory.Exists(InstallVolumePath))
            return Task.CompletedTask;

        Directory.Delete(InstallVolumePath, true);
        return Task.CompletedTask;
    }

    #endregion

    #region Virtual disks

    private async Task MountVirtualDisk()
    {
        // Check if we need to mount the virtual disk
        if (await ExecuteCommand("findmnt", RuntimeVolumePath) != 0)
        {
            await ConsoleSubSystem.WriteMoonlight("Mounting virtual disk. Please be patient");
            await ExecuteCommand("mount", $"-t auto -o loop {VirtualDiskPath} {RuntimeVolumePath}", true);
        }
        
        IsVirtualDiskMounted = true;
    }

    private async Task UnmountVirtualDisk()
    {
        // Check if we need to unmount the virtual disk
        if (await ExecuteCommand("findmnt", RuntimeVolumePath) != 0)
            return;

        await ExecuteCommand("umount", $"{RuntimeVolumePath}");
    }

    private async Task EnsureVirtualDiskCreated()
    {
        // TODO: Handle resize
        
        if(File.Exists(VirtualDiskPath))
            return;

        // Create the image file and adjust the size
        await ConsoleSubSystem.WriteMoonlight("Creating virtual disk file. Please be patient");

        var fileStream = File.Open(VirtualDiskPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        fileStream.SetLength(
            ByteConverter.FromMegaBytes(Configuration.Disk).Bytes
        );

        await fileStream.FlushAsync();
        fileStream.Close();
        await fileStream.DisposeAsync();

        // Now we want to format it
        await ConsoleSubSystem.WriteMoonlight("Formatting virtual disk. This can take a bit");

        await ExecuteCommand("mkfs", $"-t ext4 {VirtualDiskPath}", true);
    }

    private async Task<int> ExecuteCommand(string command, string arguments, bool handleExitCode = false)
    {
        var psi = new ProcessStartInfo()
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        var process = Process.Start(psi);

        if (process == null)
            throw new AggregateException("The spawned process reference is null");

        await process.WaitForExitAsync();

        if (process.ExitCode == 0 || !handleExitCode)
            return process.ExitCode;

        var output = await process.StandardOutput.ReadToEndAsync();
        output += await process.StandardError.ReadToEndAsync();

        throw new Exception($"The command {command} failed: {output}");
    }

    #endregion

    public override ValueTask DisposeAsync()
    {
        if (SecureFileSystem != null && !SecureFileSystem.IsDisposed)
            SecureFileSystem.Dispose();

        return ValueTask.CompletedTask;
    }
}