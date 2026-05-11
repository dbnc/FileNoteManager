namespace FileNoteManager.Core.Repositories;

/// <summary>
/// Entity representing a tag in the database
/// </summary>
public class TagEntity
{
    /// <summary>
    /// The tag name (primary key)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// When the tag was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Number of notes using this tag
    /// </summary>
    public int UsageCount { get; set; }
}
