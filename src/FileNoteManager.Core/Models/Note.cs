namespace FileNoteManager.Core.Models;

/// <summary>
/// Represents a note attached to a file or folder
/// </summary>
public class Note
{
    /// <summary>
    /// The full path of the file or folder
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// The note content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Tags associated with this note
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the note was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether the note is marked as deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Whether the path refers to a folder (true) or file (false)
    /// </summary>
    public bool IsFolder { get; set; }
}
