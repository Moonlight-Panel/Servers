using System.Diagnostics;
using Mono.Unix.Native;
using MoonCore.Exceptions;
using MoonCore.Helpers;
using MoonCore.Unix.Exceptions;
using MoonCore.Unix.SecureFs;
using MoonlightServers.Daemon.Configuration;
using MoonlightServers.Daemon.Helpers;

namespace MoonlightServers.Daemon.ServerSystem.SubSystems;

public class StorageSubSystem : ServerSubSystem
{
    private readonly AppConfiguration AppConfiguration;

    private SecureFileSystem SecureFileSystem;
    private ServerFileSystem ServerFileSystem;
    private ConsoleSubSystem ConsoleSubSystem;

    public string RuntimeVolumePath { get; private set; }
    public string InstallVolumePath { get; private set; }
    public string VirtualDiskPath { get; private set; }

    public bool IsVirtualDiskMounted { get; private set; }
    public bool IsInitialized { get; private set; } = false;
    public bool IsFileSystemAccessorCreated { get; private set; } = false;

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

    public override async Task Initialize()
    {
        ConsoleSubSystem = Server.GetRequiredSubSystem<ConsoleSubSystem>();

        try
        {
            await Reinitialize();
        }
        catch (Exception e)
        {
            await ConsoleSubSystem.WriteMoonlight(
                "Unable to initialize server file system. Please contact the administrator"
            );

            Logger.LogError("An unhandled error occured while lazy initializing server file system: {e}", e);

            throw;
        }
    }

    public override async Task Delete()
    {
        if (Configuration.UseVirtualDisk)
            await DeleteVirtualDisk();

        await DeleteRuntimeVolume();
        await DeleteInstallVolume();
    }

    public async Task Reinitialize()
    {
        if (IsInitialized && StateMachine.State != ServerState.Offline)
        {
            throw new HttpApiException(
                "Unable to reinitialize storage sub system while the server is not offline",
                400
            );
        }

        IsInitialized = false;

        await EnsureRuntimeVolumeCreated();

        if (Configuration.UseVirtualDisk)
        {
            // Load the state of a possible mount already existing.
            // This ensures we are aware of the mount state after a restart.
            // Without that we would get errors when mounting
            IsVirtualDiskMounted = await CheckVirtualDiskMounted();

            // Ensure we have the virtual disk created and in the correct size
            await EnsureVirtualDisk();
        }

        IsInitialized = true;
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
        // If the initialization is still running we don't want to allow access to the runtime volume at all
        if (!IsInitialized)
            return false;

        // If we use virtual disks and the disk isn't already mounted, we need to mount it now
        if (Configuration.UseVirtualDisk && !IsVirtualDiskMounted)
            await MountVirtualDisk();

        if (!IsFileSystemAccessorCreated)
            await CreateFileSystemAccessor();

        if (!skipPermissions)
            await EnsureRuntimePermissions();

        return true;
    }

    private Task EnsureRuntimeVolumeCreated()
    {
        // Create the volume directory if required
        if (!Directory.Exists(RuntimeVolumePath))
            Directory.CreateDirectory(RuntimeVolumePath);

        return Task.CompletedTask;
    }

    private async Task DeleteRuntimeVolume()
    {
        // Already deleted? Then we don't want to care about anything at all
        if (!Directory.Exists(RuntimeVolumePath))
            return;

        // If we use a virtual disk there are no files to delete via the
        // secure file system as the virtual disk is already gone by now
        if (Configuration.UseVirtualDisk)
        {
            Directory.Delete(RuntimeVolumePath, true);
            return;
        }

        // If we still habe a file system accessor, we reuse it :)
        if (IsFileSystemAccessorCreated)
        {
            foreach (var entry in SecureFileSystem.ReadDir("/"))
            {
                if (entry.IsFile)
                    SecureFileSystem.Remove(entry.Name);
                else
                    SecureFileSystem.RemoveAll(entry.Name);
            }

            await DestroyFileSystemAccessor();
        }
        else
        {
            // If the file system accessor has already been removed we create a temporary one.
            // This handles the case when a server was never accessed and as such there is no accessor created yet

            var sfs = new SecureFileSystem(RuntimeVolumePath);

            foreach (var entry in sfs.ReadDir("/"))
            {
                if (entry.IsFile)
                    sfs.Remove(entry.Name);
                else
                    sfs.RemoveAll(entry.Name);
            }

            sfs.Dispose();
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

        // Chown all content of the runtime volume
        foreach (var entry in SecureFileSystem.ReadDir("/"))
        {
            if (entry.IsFile)
                SecureFileSystem.Chown(entry.Name, uid, gid);
            else
                SecureFileSystem.ChownAll(entry.Name, uid, gid);
        }

        // Chown also the main path of the volume
        if (Syscall.chown(RuntimeVolumePath, uid, gid) != 0)
        {
            var error = Stdlib.GetLastError();
            throw new SyscallException(error, "An error occured while chowning runtime volume");
        }

        return Task.CompletedTask;
    }

    private Task CreateFileSystemAccessor()
    {
        SecureFileSystem = new(RuntimeVolumePath);
        ServerFileSystem = new(SecureFileSystem);

        IsFileSystemAccessorCreated = true;

        return Task.CompletedTask;
    }

    private Task DestroyFileSystemAccessor()
    {
        if (!SecureFileSystem.IsDisposed)
            SecureFileSystem.Dispose();

        IsFileSystemAccessorCreated = false;

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
        await ConsoleSubSystem.WriteMoonlight("Mounting virtual disk");
        await ExecuteCommand("mount", $"-t auto -o loop {VirtualDiskPath} {RuntimeVolumePath}", true);

        IsVirtualDiskMounted = true;
    }

    private async Task UnmountVirtualDisk()
    {
        await ConsoleSubSystem.WriteMoonlight("Unmounting virtual disk");
        await ExecuteCommand("umount", RuntimeVolumePath, handleExitCode: true);

        IsVirtualDiskMounted = false;
    }

    private async Task<bool> CheckVirtualDiskMounted()
        => await ExecuteCommand("findmnt", RuntimeVolumePath) == 0;

    private async Task EnsureVirtualDisk()
    {
        var existingDiskInfo = new FileInfo(VirtualDiskPath);

        // Check if we need to create the disk or just check for the size
        if (existingDiskInfo.Exists)
        {
            var expectedSize = ByteConverter.FromMegaBytes(Configuration.Disk).Bytes;
            
            // If the disk size matches, we are done here
            if (expectedSize == existingDiskInfo.Length)
            {
                Logger.LogDebug("Virtual disk size matches expected size");
                return;
            }

            // We cant resize while the server is running as this would lead to possible file corruptions
            // and crashes of the software the server is running
            if (StateMachine.State != ServerState.Offline)
            {
                Logger.LogDebug("Skipping disk resizing while server is not offline");
                await ConsoleSubSystem.WriteMoonlight("Skipping disk resizing as the server is not offline");
                return;
            }

            if (expectedSize > existingDiskInfo.Length)
            {
                Logger.LogDebug("Detected smaller disk size as expected. Resizing now");
                await ConsoleSubSystem.WriteMoonlight("Preparing to resize virtual disk");

                // If the file system accessor is still open we need to destroy it in order to to resize
                if (IsFileSystemAccessorCreated)
                    await DestroyFileSystemAccessor();

                // If the disk is still mounted we need to unmount it in order to resize
                if (IsVirtualDiskMounted)
                    await UnmountVirtualDisk();

                // Resize the disk image file
                Logger.LogDebug("Resizing virtual disk file");

                var fileStream = File.Open(VirtualDiskPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                fileStream.SetLength(expectedSize);

                await fileStream.FlushAsync();
                fileStream.Close();
                await fileStream.DisposeAsync();

                // Now we need to run the file system check on the disk
                Logger.LogDebug("Checking virtual disk for corruptions using e2fsck");
                await ConsoleSubSystem.WriteMoonlight("Checking virtual disk for any corruptions");

                await ExecuteCommand(
                    "e2fsck",
                    $"{AppConfiguration.Storage.VirtualDiskOptions.E2FsckParameters} {VirtualDiskPath}",
                    handleExitCode: true
                );

                // Resize the file system
                Logger.LogDebug("Resizing filesystem of virtual disk using resize2fs");
                await ConsoleSubSystem.WriteMoonlight("Resizing virtual disk");

                await ExecuteCommand("resize2fs", VirtualDiskPath, handleExitCode: true);

                // Done :>
                Logger.LogDebug("Successfully resized virtual disk");
                await ConsoleSubSystem.WriteMoonlight("Resize of virtual disk completed");
            }
            else if (existingDiskInfo.Length > expectedSize)
            {
                Logger.LogDebug("Shrink from {expected} to {existing} detected", expectedSize, existingDiskInfo.Length);

                await ConsoleSubSystem.WriteMoonlight(
                    "Unable to shrink virtual disk. Virtual disk will stay unmodified"
                );

                Logger.LogWarning(
                    "Server disk limit was lower then the size of the virtual disk. Virtual disk wont be resized to prevent loss of files");
            }
        }
        else
        {
            // Create the image file and adjust the size
            Logger.LogDebug("Creating virtual disk");
            await ConsoleSubSystem.WriteMoonlight("Creating virtual disk file. Please be patient");

            var fileStream = File.Open(VirtualDiskPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            fileStream.SetLength(
                ByteConverter.FromMegaBytes(Configuration.Disk).Bytes
            );

            await fileStream.FlushAsync();
            fileStream.Close();
            await fileStream.DisposeAsync();

            // Now we want to format it
            Logger.LogDebug("Formatting virtual disk");
            await ConsoleSubSystem.WriteMoonlight("Formatting virtual disk. This can take a bit");

            await ExecuteCommand(
                "mkfs",
                $"-t {AppConfiguration.Storage.VirtualDiskOptions.FileSystemType} {VirtualDiskPath}",
                handleExitCode: true
            );

            // Done :)
            Logger.LogDebug("Successfully created virtual disk");
            await ConsoleSubSystem.WriteMoonlight("Virtual disk created");
        }
    }

    private async Task DeleteVirtualDisk()
    {
        if (IsFileSystemAccessorCreated)
            await DestroyFileSystemAccessor();

        if (IsVirtualDiskMounted)
            await UnmountVirtualDisk();

        File.Delete(VirtualDiskPath);
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

    public override async ValueTask DisposeAsync()
    {
        // We check for that just to ensure that we no longer access the file system 
        if (IsFileSystemAccessorCreated)
            await DestroyFileSystemAccessor();
    }
}