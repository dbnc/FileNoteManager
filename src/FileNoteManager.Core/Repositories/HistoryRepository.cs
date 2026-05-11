namespace FileNoteManager.Core.Repositories;

/// <summary>
/// Entity representing a note in the database
/// </summary>
public class NoteEntity
{
    /// <summary>
    /// The file or folder path (primary key)
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// The note content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the note was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether the note is soft-deleted
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Whether this is a folder note
    /// </summary>
    public bool IsFolder { get; set; }
}
