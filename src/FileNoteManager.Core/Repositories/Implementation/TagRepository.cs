using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Repositories.Implementation;

/// <summary>
/// Implementation of tag repository using SQLite
/// </summary>
public class TagRepository : ITagRepository
{
    private readonly DatabaseManager _dbManager;
    
    public TagRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }
    
    public TagEntity? GetByName(string name)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.QueryFirstOrDefault<TagEntity>(
            "SELECT * FROM TagEntity WHERE Name = @Name",
            new { Name = name }
        );
    }
    
    public IEnumerable<TagEntity> GetAll()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<TagEntity>(
            "SELECT * FROM TagEntity ORDER BY UsageCount DESC, Name ASC"
        );
    }
    
    public void Save(TagEntity tag)
    {
        using var connection = _dbManager.CreateConnection();
        
        var existing = GetByName(tag.Name);
        
        if (existing == null)
        {
            connection.Execute(
                @"INSERT INTO TagEntity (Name, CreatedAt, UsageCount)
                  VALUES (@Name, @CreatedAt, @UsageCount)",
                tag
            );
        }
        else
        {
            connection.Execute(
                @"UPDATE TagEntity SET UsageCount = @UsageCount WHERE Name = @Name",
                tag
            );
        }
    }
    
    public void Delete(string name)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            "DELETE FROM TagEntity WHERE Name = @Name",
            new { Name = name }
        );
    }
    
    public IEnumerable<TagEntity> GetMostUsed(int count)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<TagEntity>(
            "SELECT * FROM TagEntity ORDER BY UsageCount DESC, Name ASC LIMIT @Count",
            new { Count = count }
        );
    }
    
    public void IncrementUsage(string name)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            "UPDATE TagEntity SET UsageCount = UsageCount + 1 WHERE Name = @Name",
            new { Name = name }
        );
    }
    
    public void DecrementUsage(string name)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            "UPDATE TagEntity SET UsageCount = MAX(0, UsageCount - 1) WHERE Name = @Name",
            new { Name = name }
        );
    }
}
