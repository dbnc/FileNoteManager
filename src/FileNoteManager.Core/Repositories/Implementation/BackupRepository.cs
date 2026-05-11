using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Repositories.Implementation;

/// <summary>
/// Implementation of backup record repository using SQLite
/// </summary>
public class BackupRepository : IBackupRepository
{
    private readonly DatabaseManager _dbManager;
    
    public BackupRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }
    
    public IEnumerable<BackupRecordEntity> GetAll()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<BackupRecordEntity>(
            "SELECT * FROM BackupRecordEntity ORDER BY CreatedAt DESC"
        );
    }
    
    public BackupRecordEntity? GetMostRecent()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.QueryFirstOrDefault<BackupRecordEntity>(
            "SELECT * FROM BackupRecordEntity ORDER BY CreatedAt DESC LIMIT 1"
        );
    }
    
    public void Save(BackupRecordEntity record)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            @"INSERT INTO BackupRecordEntity (BackupPath, CreatedAt, FileSize)
              VALUES (@BackupPath, @CreatedAt, @FileSize)",
            record
        );
    }
    
    public void Delete(int id)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            "DELETE FROM BackupRecordEntity WHERE Id = @Id",
            new { Id = id }
        );
    }
    
    public int GetCount()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM BackupRecordEntity"
        );
    }
}
