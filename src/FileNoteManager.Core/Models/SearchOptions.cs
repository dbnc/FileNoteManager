namespace FileNoteManager.Core.Models;

/// <summary>
/// Options for search operations
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Whether to search in note content
    /// </summary>
    public bool IncludeContent { get; set; } = true;
    
    /// <summary>
    /// Whether to search in tags
    /// </summary>
    public bool IncludeTags { get; set; } = true;
    
    /// <summary>
    /// Whether to search in file paths
    /// </summary>
    public bool IncludePath { get; set; } = false;
    
    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 100;
}
