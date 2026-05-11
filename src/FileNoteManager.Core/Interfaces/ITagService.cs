using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for managing tags
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Get all tags in the system
    /// </summary>
    IEnumerable<Tag> GetAllTags();
    
    /// <summary>
    /// Get a tag by name
    /// </summary>
    Tag? GetTagByName(string name);
    
    /// <summary>
    /// Get the most frequently used tags
    /// </summary>
    IEnumerable<Tag> GetMostUsedTags(int count);
    
    /// <summary>
    /// Get all notes with a specific tag
    /// </summary>
    IEnumerable<Note> GetNotesByTag(string tagName);
    
    /// <summary>
    /// Get tag usage frequency for tag cloud
    /// </summary>
    Dictionary<string, int> GetTagFrequency();
}
