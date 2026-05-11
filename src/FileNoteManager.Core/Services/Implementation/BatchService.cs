using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of batch operations for notes
/// </summary>
public class BatchService
{
    private readonly INoteService _noteService;
    
    public BatchService(INoteService noteService)
    {
        _noteService = noteService;
    }
    
    /// <summary>
    /// Create notes for multiple paths with the same content
    /// </summary>
    public void BatchCreateNotes(IEnumerable<string> paths, string content, IEnumerable<string> tags)
    {
        foreach (var path in paths)
        {
            try
            {
                _noteService.CreateNote(path, content, tags);
            }
            catch (ArgumentException)
            {
                // Skip if content is blank
            }
        }
    }
    
    /// <summary>
    /// Update notes for multiple paths with the same content
    /// </summary>
    public void BatchUpdateNotes(IEnumerable<string> paths, string content, IEnumerable<string> tags)
    {
        foreach (var path in paths)
        {
            try
            {
                _noteService.UpdateNote(path, content, tags);
            }
            catch (ArgumentException)
            {
                // Skip if content is blank
            }
        }
    }
    
    /// <summary>
    /// Create or update notes individually for each path
    /// </summary>
    public void BatchSetNotes(Dictionary<string, (string Content, IEnumerable<string> Tags)> notesData)
    {
        foreach (var kvp in notesData)
        {
            var path = kvp.Key;
            var (content, tags) = kvp.Value;
            
            try
            {
                if (_noteService.HasNote(path))
                {
                    _noteService.UpdateNote(path, content, tags);
                }
                else
                {
                    _noteService.CreateNote(path, content, tags);
                }
            }
            catch (ArgumentException)
            {
                // Skip if content is blank
            }
        }
    }
    
    /// <summary>
    /// Delete notes for multiple paths
    /// </summary>
    public void BatchDeleteNotes(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _noteService.DeleteNote(path);
        }
    }
}
