namespace FileNoteManager.Core.Models;

/// <summary>
/// Represents a tag that can be associated with notes
/// </summary>
public class Tag
{
    /// <summary>
    /// The tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// When the tag was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Number of notes using this tag
    /// </summary>
    public int UsageCount { get; set; }
}
