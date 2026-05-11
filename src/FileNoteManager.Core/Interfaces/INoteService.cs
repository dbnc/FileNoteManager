using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for managing notes
/// </summary>
public interface INoteService
{
    /// <summary>
    /// Get a note by its path
    /// </summary>
    Note? GetNote(string path);
    
    /// <summary>
    /// Create a new note
    /// </summary>
    Note CreateNote(string path, string content, IEnumerable<string> tags);
    
    /// <summary>
    /// Update an existing note
    /// </summary>
    Note UpdateNote(string path, string content, IEnumerable<string> tags);
    
    /// <summary>
    /// Delete a note
    /// </summary>
    void DeleteNote(string path);
    
    /// <summary>
    /// Search notes by keyword using full-text search
    /// </summary>
    IEnumerable<Note> SearchNotes(string keyword);
    
    /// <summary>
    /// Search notes by tags
    /// </summary>
    IEnumerable<Note> SearchByTags(IEnumerable<string> tags);
    
    /// <summary>
    /// Update the path of a note (for file move/rename)
    /// </summary>
    void UpdatePath(string oldPath, string newPath);
    
    /// <summary>
    /// Mark a note as deleted (soft delete)
    /// </summary>
    void MarkAsDeleted(string path);
    
    /// <summary>
    /// Check if a note exists for the given path
    /// </summary>
    bool HasNote(string path);

    /// <summary>
    /// Try to find a soft-deleted note with the same file/folder name as newPath
    /// and migrate it to newPath (un-delete + update path).
    /// Returns the migrated note, or null if migration was not possible
    /// (no match, or multiple ambiguous matches, or active note already at newPath).
    /// </summary>
    Note? TryMigrateNote(string newPath);
}
