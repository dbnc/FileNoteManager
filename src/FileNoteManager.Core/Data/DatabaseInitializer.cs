using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Data;

/// <summary>
/// Handles database schema initialization and migrations
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;
    
    public DatabaseInitializer(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }
    
    /// <summary>
    /// Initialize the database with the required schema
    /// </summary>
    public void Initialize()
    {
        EnsureDirectoryExists();
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        CreateNotesTable(connection);
        CreateTagsTable(connection);
        CreateNoteTagTable(connection);
        CreateHistoryTable(connection);
        CreateBackupRecordTable(connection);
        CreateFtsTable(connection);
        CreateIndexes(connection);
        CreateTriggers(connection);
    }
    
    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_connectionString.Replace("Data Source=", ""));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
    
    private void CreateNotesTable(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS NoteEntity (
                Path TEXT PRIMARY KEY NOT NULL,
                Content TEXT,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                IsFolder INTEGER NOT NULL DEFAULT 0
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateTagsTable(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS TagEntity (
                Name TEXT PRIMARY KEY NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UsageCount INTEGER NOT NULL DEFAULT 0
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateNoteTagTable(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS NoteTagEntity (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NotePath TEXT NOT NULL,
                TagName TEXT NOT NULL,
                FOREIGN KEY (NotePath) REFERENCES NoteEntity(Path) ON DELETE CASCADE,
                FOREIGN KEY (TagName) REFERENCES TagEntity(Name) ON DELETE CASCADE
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateHistoryTable(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS HistoryEntity (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NotePath TEXT NOT NULL,
                Content TEXT,
                SavedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                VersionNumber INTEGER NOT NULL
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateBackupRecordTable(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS BackupRecordEntity (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BackupPath TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FileSize INTEGER NOT NULL
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateFtsTable(SqliteConnection connection)
    {
        // FTS5 virtual table for full-text search
        var sql = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS NoteFTS USING FTS5(
                Path,
                Content,
                Tags,
                content=''
            )";
        
        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
    
    private void CreateIndexes(SqliteConnection connection)
    {
        var indexes = new[]
        {
            "CREATE INDEX IF NOT EXISTS idx_note_updated ON NoteEntity(UpdatedAt DESC)",
            "CREATE INDEX IF NOT EXISTS idx_note_deleted ON NoteEntity(IsDeleted)",
            "CREATE INDEX IF NOT EXISTS idx_notetag_note ON NoteTagEntity(NotePath)",
            "CREATE INDEX IF NOT EXISTS idx_notetag_tag ON NoteTagEntity(TagName)",
            "CREATE INDEX IF NOT EXISTS idx_history_path ON HistoryEntity(NotePath)",
            "CREATE INDEX IF NOT EXISTS idx_tag_usage ON TagEntity(UsageCount DESC)"
        };
        
        foreach (var indexSql in indexes)
        {
            using var command = new SqliteCommand(indexSql, connection);
            command.ExecuteNonQuery();
        }
    }
    
    private void CreateTriggers(SqliteConnection connection)
    {
        // Trigger to update FTS on note insert
        var insertTrigger = @"
            CREATE TRIGGER IF NOT EXISTS trg_note_insert_fts
            AFTER INSERT ON NoteEntity
            BEGIN
                INSERT INTO NoteFTS(Path, Content, Tags)
                SELECT NEW.Path, NEW.Content, 
                       COALESCE(
                           (SELECT GROUP_CONCAT(t.Name, ' ')
                            FROM NoteTagEntity nt
                            JOIN TagEntity t ON t.Name = nt.TagName
                            WHERE nt.NotePath = NEW.Path), 
                           ''
                       );
            END";
        
        // Trigger to update FTS on note update
        var updateTrigger = @"
            CREATE TRIGGER IF NOT EXISTS trg_note_update_fts
            AFTER UPDATE ON NoteEntity
            BEGIN
                DELETE FROM NoteFTS WHERE Path = OLD.Path;
                INSERT INTO NoteFTS(Path, Content, Tags)
                SELECT NEW.Path, NEW.Content,
                       COALESCE(
                           (SELECT GROUP_CONCAT(t.Name, ' ')
                            FROM NoteTagEntity nt
                            JOIN TagEntity t ON t.Name = nt.TagName
                            WHERE nt.NotePath = NEW.Path), 
                           ''
                       );
            END";
        
        // Trigger to delete from FTS on note delete
        var deleteTrigger = @"
            CREATE TRIGGER IF NOT EXISTS trg_note_delete_fts
            AFTER DELETE ON NoteEntity
            BEGIN
                DELETE FROM NoteFTS WHERE Path = OLD.Path;
            END";
        
        using (var command = new SqliteCommand(insertTrigger, connection))
        {
            command.ExecuteNonQuery();
        }
        
        using (var command = new SqliteCommand(updateTrigger, connection))
        {
            command.ExecuteNonQuery();
        }
        
        using (var command = new SqliteCommand(deleteTrigger, connection))
        {
            command.ExecuteNonQuery();
        }
    }
}
