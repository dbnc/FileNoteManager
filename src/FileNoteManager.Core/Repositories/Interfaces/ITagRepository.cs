namespace FileNoteManager.Core.Repositories.Interfaces;

/// <summary>
/// Repository interface for tag entities
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Get a tag by name
    /// </summary>
    TagEntity? GetByName(string name);
    
    /// <summary>
    /// Get all tags
    /// </summary>
    IEnumerable<TagEntity> GetAll();
    
    /// <summary>
    /// Save a tag (insert or update)
    /// </summary>
    void Save(TagEntity tag);
    
    /// <summary>
    /// Delete a tag by name
    /// </summary>
    void Delete(string name);
    
    /// <summary>
    /// Get the most used tags
    /// </summary>
    IEnumerable<TagEntity> GetMostUsed(int count);
    
    /// <summary>
    /// Increment usage count for a tag
    /// </summary>
    void IncrementUsage(string name);
    
    /// <summary>
    /// Decrement usage count for a tag
    /// </summary>
    void DecrementUsage(string name);
}
