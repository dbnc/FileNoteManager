namespace FileNoteManager.Core.Repositories;

/// <summary>
/// Entity representing a backup record in the database
/// </summary>
public class BackupRecordEntity
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Path to the backup file
    /// </summary>
    public string BackupPath { get; set; } = string.Empty;
    
    /// <summary>
    /// When the backup was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Size of the backup file in bytes
    /// </summary>
    public long FileSize { get; set; }
}
