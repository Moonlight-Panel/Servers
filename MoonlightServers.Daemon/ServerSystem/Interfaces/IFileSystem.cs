namespace MoonlightServers.Daemon.ServerSystem.Interfaces;

public interface IFileSystem : IServerComponent
{
    /// <summary>
    /// Gets the path of the file system on the host operating system to be reused by other components
    /// </summary>
    /// <returns>Path to the file systems storage location</returns>
    public Task<string> GetPathAsync();
    
    /// <summary>
    /// Checks if the file system exists
    /// </summary>
    /// <returns>True if it does exist. False if it doesn't exist</returns>
    public Task<bool> CheckExistsAsync();
    
    /// <summary>
    /// Checks if the file system is mounted
    /// </summary>
    /// <returns>True if its mounted, False if it is not mounted</returns>
    public Task<bool> CheckMountedAsync();

    /// <summary>
    /// Creates the file system. E.g. Creating a virtual disk, formatting it
    /// </summary>
    /// <returns></returns>
    public Task CreateAsync();

    /// <summary>
    /// Performs checks and optimisations on the file system.
    /// E.g. checking for corrupted files, resizing a virtual disk or adjusting file permissions
    /// <remarks>Requires <see cref="MountAsync"/> to be called before or the file system to be in a mounted state</remarks>
    /// </summary>
    /// <returns></returns>
    public Task PerformChecksAsync();
    
    /// <summary>
    /// Mounts the file system
    /// </summary>
    /// <returns></returns>
    public Task MountAsync();
    
    /// <summary>
    /// Unmounts the file system
    /// </summary>
    /// <returns></returns>
    public Task UnmountAsync();

    /// <summary>
    /// Destroys the file system and its contents
    /// </summary>
    /// <returns></returns>
    public Task DestroyAsync();
}