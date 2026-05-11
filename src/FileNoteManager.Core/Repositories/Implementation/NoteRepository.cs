using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Repositories.Implementation;

/// <summary>
/// Implementation of note repository using SQLite
/// </summary>
public class NoteRepository : INoteRepository
{
    private readonly DatabaseManager _dbManager;
    
    public NoteRepository(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }
    
    public NoteEntity? GetById(string path)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.QueryFirstOrDefault<NoteEntity>(
            "SELECT * FROM NoteEntity WHERE Path = @Path",
            new { Path = path }
        );
    }
    
    public IEnumerable<NoteEntity> GetAll()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<NoteEntity>(
            "SELECT * FROM NoteEntity WHERE IsDeleted = 0 ORDER BY UpdatedAt DESC"
        ).ToList(); // force-buffer inside the using block
    }
    
    public void Save(NoteEntity note)
    {
        using var connection = _dbManager.CreateConnection();
        
        var existing = GetById(note.Path);
        
        if (existing == null)
        {
            connection.Execute(
                @"INSERT INTO NoteEntity (Path, Content, CreatedAt, UpdatedAt, IsDeleted, IsFolder)
                  VALUES (@Path, @Content, @CreatedAt, @UpdatedAt, @IsDeleted, @IsFolder)",
                note
            );
        }
        else
        {
            connection.Execute(
                @"UPDATE NoteEntity 
                  SET Content = @Content, UpdatedAt = @UpdatedAt, IsDeleted = @IsDeleted, IsFolder = @IsFolder
                  WHERE Path = @Path",
                note
            );
        }
    }
    
    public void Delete(string path)
    {
        using var connection = _dbManager.CreateConnection();
        connection.Execute(
            "DELETE FROM NoteEntity WHERE Path = @Path",
            new { Path = path }
        );
    }
    
    public IEnumerable<NoteEntity> Search(string keyword)
    {
        using var connection = _dbManager.CreateConnection();
        
        // Use FTS5 for full-text search
        return connection.Query<NoteEntity>(
            @"SELECT n.* FROM NoteEntity n
              INNER JOIN NoteFTS fts ON n.Path = fts.Path
              WHERE NoteFTS MATCH @Keyword AND n.IsDeleted = 0
              ORDER BY n.UpdatedAt DESC",
            new { Keyword = keyword }
        ).ToList(); // force-buffer inside the using block
    }
    
    public void UpdatePath(string oldPath, string newPath)
    {
        using var connection = _dbManager.CreateConnection();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Update note path
            connection.Execute(
                "UPDATE NoteEntity SET Path = @NewPath WHERE Path = @OldPath",
                new { NewPath = newPath, OldPath = oldPath },
                transaction
            );
            
            // Update note-tag relationships
            connection.Execute(
                "UPDATE NoteTagEntity SET NotePath = @NewPath WHERE NotePath = @OldPath",
                new { NewPath = newPath, OldPath = oldPath },
                transaction
            );
            
            // Update history records
            connection.Execute(
                "UPDATE HistoryEntity SET NotePath = @NewPath WHERE NotePath = @OldPath",
                new { NewPath = newPath, OldPath = oldPath },
                transaction
            );
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    public int GetCount()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM NoteEntity WHERE IsDeleted = 0"
        );
    }
    
    public int GetFileCount()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM NoteEntity WHERE IsDeleted = 0 AND IsFolder = 0"
        );
    }
    
    public int GetFolderCount()
    {
        using var connection = _dbManager.CreateConnection();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM NoteEntity WHERE IsDeleted = 0 AND IsFolder = 1"
        );
    }

    public IEnumerable<NoteEntity> FindDeletedByFileName(string fileName)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<NoteEntity>(
            @"SELECT * FROM NoteEntity
              WHERE IsDeleted = 1
                AND (Path LIKE '%\' || @Name
                  OR Path LIKE '%/' || @Name
                  OR Path = @Name)
              ORDER BY UpdatedAt DESC",
            new { Name = fileName }
        ).ToList();
    }

    public IEnumerable<NoteEntity> FindActiveByFileName(string fileName)
    {
        using var connection = _dbManager.CreateConnection();
        return connection.Query<NoteEntity>(
            @"SELECT * FROM NoteEntity
              WHERE IsDeleted = 0
                AND (Path LIKE '%\' || @Name
                  OR Path LIKE '%/' || @Name
                  OR Path = @Name)
              ORDER BY UpdatedAt DESC",
            new { Name = fileName }
        ).ToList();
    }
}
