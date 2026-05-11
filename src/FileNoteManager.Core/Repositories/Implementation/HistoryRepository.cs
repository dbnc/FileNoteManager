using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Repositories.Implementation;

/// <summary>
/// Implementation of history repository using SQLite
/// </summary>
public class HistoryRepository : IHistoryRepository
{
    private readonly DatabaseManager _dbManager;
    
    public HistoryRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }
    
    public IEnumerable<HistoryEntity> GetByPath(string path)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<HistoryEntity>(
            "SELECT * FROM HistoryEntity WHERE NotePath = @Path ORDER BY VersionNumber DESC",
            new { Path = path }
        );
    }
    
    public HistoryEntity? GetById(int id)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.QueryFirstOrDefault<HistoryEntity>(
            "SELECT * FROM HistoryEntity WHERE Id = @Id",
            new { Id = id }
        );
    }
    
    public void Save(HistoryEntity history)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            @"INSERT INTO HistoryEntity (NotePath, Content, SavedAt, VersionNumber)
              VALUES (@NotePath, @Content, @SavedAt, @VersionNumber)",
            history
        );
    }
    
    public void DeleteOldVersions(string path, int keepVersions)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            @"DELETE FROM HistoryEntity 
              WHERE NotePath = @Path 
              AND Id NOT IN (
                  SELECT Id FROM HistoryEntity 
                  WHERE NotePath = @Path 
                  ORDER BY VersionNumber DESC 
                  LIMIT @KeepVersions
              )",
            new { Path = path, KeepVersions = keepVersions }
        );
    }
    
    public int GetNextVersionNumber(string path)
    {
        using var connection = _dbManager.CreateConnection();
        var maxVersion = connection.ExecuteScalar<int?>(
            "SELECT MAX(VersionNumber) FROM HistoryEntity WHERE NotePath = @Path",
            new { Path = path }
        );
        
        return (maxVersion ?? 0) + 1;
    }
}
