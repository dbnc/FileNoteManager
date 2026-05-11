namespace FileNoteManager.Core.Repositories;

/// <summary>
/// Entity representing a history record in the database
/// </summary>
public class HistoryEntity
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The path of the note
    /// </summary>
    public string NotePath { get; set; } = string.Empty;
    
    /// <summary>
    /// The content at this point in history
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// When this version was saved
    /// </summary>
    public DateTime SavedAt { get; set; }
    
    /// <summary>
    /// Version number
    /// </summary>
    public int VersionNumber { get; set; }
}
