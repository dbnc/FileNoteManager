namespace FileNoteManager.Core.Repositories.Interfaces;

/// <summary>
/// Repository interface for backup record entities
/// </summary>
public interface IBackupRepository
{
    /// <summary>
    /// Get all backup records
    /// </summary>
    IEnumerable<BackupRecordEntity> GetAll();
    
    /// <summary>
    /// Get the most recent backup record
    /// </summary>
    BackupRecordEntity? GetMostRecent();
    
    /// <summary>
    /// Save a backup record
    /// </summary>
    void Save(BackupRecordEntity record);
    
    /// <summary>
    /// Delete a backup record by ID
    /// </summary>
    void Delete(int id);
    
    /// <summary>
    /// Get the count of backup records
    /// </summary>
    int GetCount();
}
