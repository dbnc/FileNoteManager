using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for managing note history
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// Get all history versions for a note
    /// </summary>
    IEnumerable<NoteHistory> GetHistory(string path);
    
    /// <summary>
    /// Get a specific history version
    /// </summary>
    NoteHistory? GetVersion(string path, int versionId);
    
    /// <summary>
    /// Save a history version before updating
    /// </summary>
    void SaveHistory(string path, string content);
    
    /// <summary>
    /// Restore a note to a specific version
    /// </summary>
    void RestoreVersion(string path, int versionId);
    
    /// <summary>
    /// Clean up old versions, keeping only the most recent ones
    /// </summary>
    void CleanupOldVersions(string path, int maxVersions = 10);
}
