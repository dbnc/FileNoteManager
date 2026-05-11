using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Repositories.Implementation;

/// <summary>
/// Implementation of note-tag relationship repository using SQLite
/// </summary>
public class NoteTagRepository : INoteTagRepository
{
    private readonly DatabaseManager _dbManager;
    private readonly ITagRepository _tagRepository;
    
    public NoteTagRepository(DatabaseManager dbManager, ITagRepository tagRepository)
    {
        _dbManager = dbManager;
        _tagRepository = tagRepository;
    }
    
    public IEnumerable<string> GetTagsForNote(string notePath)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<string>(
            "SELECT TagName FROM NoteTagEntity WHERE NotePath = @NotePath",
            new { NotePath = notePath }
        ).ToList(); // force-buffer inside the using block
    }
    
    public IEnumerable<string> GetNotesForTag(string tagName)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<string>(
            @"SELECT nt.NotePath FROM NoteTagEntity nt
              INNER JOIN NoteEntity n ON n.Path = nt.NotePath
              WHERE nt.TagName = @TagName AND n.IsDeleted = 0",
            new { TagName = tagName }
        ).ToList(); // force-buffer inside the using block
    }
    
    public void AddTagToNote(string notePath, string tagName)
    {
        using var connection = _dbManager.CreateConnection();
        
        // Check if the relationship already exists
        var exists = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM NoteTagEntity WHERE NotePath = @NotePath AND TagName = @TagName",
            new { NotePath = notePath, TagName = tagName }
        );
        
        if (exists == 0)
        {
            connection.Execute(
                "INSERT INTO NoteTagEntity (NotePath, TagName) VALUES (@NotePath, @TagName)",
                new { NotePath = notePath, TagName = tagName }
            );
            
            // Increment tag usage
            _tagRepository.IncrementUsage(tagName);
        }
    }
    
    public void RemoveTagFromNote(string notePath, string tagName)
    {
        using var connection = _dbManager.CreateConnection();
        
        var affected = connection.Execute(
            "DELETE FROM NoteTagEntity WHERE NotePath = @NotePath AND TagName = @TagName",
            new { NotePath = notePath, TagName = tagName }
        );
        
        if (affected > 0)
        {
            // Decrement tag usage
            _tagRepository.DecrementUsage(tagName);
        }
    }
    
    public void RemoveAllTagsFromNote(string notePath)
    {
        using var connection = _dbManager.CreateConnection();
        
        // Get all tags for this note first
        var tags = GetTagsForNote(notePath).ToList();
        
        // Delete all relationships
        connection.Execute(
            "DELETE FROM NoteTagEntity WHERE NotePath = @NotePath",
            new { NotePath = notePath }
        );
        
        // Decrement usage for all tags
        foreach (var tag in tags)
        {
            _tagRepository.DecrementUsage(tag);
        }
    }
    
    public Dictionary<string, int> GetTagFrequency()
    {
        using var connection = _dbManager.CreateConnection();
        
        var results = connection.Query<(string Name, int Count)>(
            @"SELECT t.Name, COUNT(nt.NotePath) as Count
              FROM TagEntity t
              LEFT JOIN NoteTagEntity nt ON t.Name = nt.TagName
              LEFT JOIN NoteEntity n ON n.Path = nt.NotePath AND n.IsDeleted = 0
              GROUP BY t.Name
              ORDER BY Count DESC"
        );
        
        return results.ToDictionary(r => r.Name, r => r.Count);
    }
}
