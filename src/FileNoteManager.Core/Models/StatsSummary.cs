namespace FileNoteManager.Core.Models;

/// <summary>
/// Summary statistics for the note database
/// </summary>
public class StatsSummary
{
    /// <summary>
    /// Total number of notes
    /// </summary>
    public int TotalNotes { get; set; }
    
    /// <summary>
    /// Number of notes for files
    /// </summary>
    public int FileNotes { get; set; }
    
    /// <summary>
    /// Number of notes for folders
    /// </summary>
    public int FolderNotes { get; set; }
    
    /// <summary>
    /// Database file size in bytes
    /// </summary>
    public long DatabaseSize { get; set; }
    
    /// <summary>
    /// When the last backup was created
    /// </summary>
    public DateTime? LastBackupTime { get; set; }
}
