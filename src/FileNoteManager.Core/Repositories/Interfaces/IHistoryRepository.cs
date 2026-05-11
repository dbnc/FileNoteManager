namespace FileNoteManager.Core.Repositories.Interfaces;

/// <summary>
/// Repository interface for history entities
/// </summary>
public interface IHistoryRepository
{
    /// <summary>
    /// Get all history versions for a note
    /// </summary>
    IEnumerable<HistoryEntity> GetByPath(string path);
    
    /// <summary>
    /// Get a specific history version by ID
    /// </summary>
    HistoryEntity? GetById(int id);
    
    /// <summary>
    /// Save a history entity
    /// </summary>
    void Save(HistoryEntity history);
    
    /// <summary>
    /// Delete old versions, keeping only the most recent
    /// </summary>
    void DeleteOldVersions(string path, int keepVersions);
    
    /// <summary>
    /// Get the next version number for a path
    /// </summary>
    int GetNextVersionNumber(string path);
}
