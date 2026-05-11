namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for managing database backups
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Create a backup of the database
    /// </summary>
    void CreateBackup(string backupPath);
    
    /// <summary>
    /// Restore database from a backup
    /// </summary>
    void RestoreBackup(string backupPath);
    
    /// <summary>
    /// Get the time of the last backup
    /// </summary>
    DateTime? GetLastBackupTime();
    
    /// <summary>
    /// Clean up old backup files
    /// </summary>
    void CleanupOldBackups(int maxBackups = 5);
    
    /// <summary>
    /// Check if an automatic backup should be performed
    /// </summary>
    bool ShouldAutoBackup();
}
