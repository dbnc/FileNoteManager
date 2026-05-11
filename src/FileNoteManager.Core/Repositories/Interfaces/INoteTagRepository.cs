namespace FileNoteManager.Core.Repositories.Interfaces;

/// <summary>
/// Entity representing the many-to-many relationship between notes and tags
/// </summary>
public class NoteTagEntity
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The note path
    /// </summary>
    public string NotePath { get; set; } = string.Empty;
    
    /// <summary>
    /// The tag name
    /// </summary>
    public string TagName { get; set; } = string.Empty;
}

/// <summary>
/// Repository interface for note-tag relationships
/// </summary>
public interface INoteTagRepository
{
    /// <summary>
    /// Get all tags for a note
    /// </summary>
    IEnumerable<string> GetTagsForNote(string notePath);
    
    /// <summary>
    /// Get all notes with a specific tag
    /// </summary>
    IEnumerable<string> GetNotesForTag(string tagName);
    
    /// <summary>
    /// Add a tag to a note
    /// </summary>
    void AddTagToNote(string notePath, string tagName);
    
    /// <summary>
    /// Remove a tag from a note
    /// </summary>
    void RemoveTagFromNote(string notePath, string tagName);
    
    /// <summary>
    /// Remove all tags from a note
    /// </summary>
    void RemoveAllTagsFromNote(string notePath);
    
    /// <summary>
    /// Get tag usage frequency
    /// </summary>
    Dictionary<string, int> GetTagFrequency();
}
