namespace FileNoteManager.Core.Models;

/// <summary>
/// Represents a historical version of a note
/// </summary>
public class NoteHistory
{
    /// <summary>
    /// Unique identifier for this history entry
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The path of the note this history belongs to
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
    /// Version number (incremental)
    /// </summary>
    public int VersionNumber { get; set; }
}
