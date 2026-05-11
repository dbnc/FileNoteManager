namespace FileNoteManager.Core.Models;

/// <summary>
/// Represents a search result
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The file or folder path
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// The note content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Tags associated with the note
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// When the note was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether this is a folder
    /// </summary>
    public bool IsFolder { get; set; }
    
    /// <summary>
    /// Highlighted snippet showing the search match
    /// </summary>
    public string? Snippet { get; set; }
}
