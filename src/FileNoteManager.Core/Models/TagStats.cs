namespace FileNoteManager.Core.Models;

/// <summary>
/// Statistics for a tag
/// </summary>
public class TagStats
{
    /// <summary>
    /// The tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of notes using this tag
    /// </summary>
    public int UsageCount { get; set; }
}
