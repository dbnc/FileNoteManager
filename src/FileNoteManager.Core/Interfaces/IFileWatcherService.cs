namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Type of file system change
/// </summary>
public enum ChangeType
{
    Renamed,
    Moved,
    Deleted
}

/// <summary>
/// Arguments for file system change events
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    public string OldPath { get; set; } = string.Empty;
    public string? NewPath { get; set; }
    public ChangeType ChangeType { get; set; }
}

/// <summary>
/// Service for watching file system changes
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Start watching for file system changes
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stop watching for file system changes
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Register a path to watch
    /// </summary>
    void RegisterPath(string path);
    
    /// <summary>
    /// Unregister a path from watching
    /// </summary>
    void UnregisterPath(string path);
    
    /// <summary>
    /// Event raised when a file system change is detected
    /// </summary>
    event EventHandler<FileChangedEventArgs>? FileChanged;
}
