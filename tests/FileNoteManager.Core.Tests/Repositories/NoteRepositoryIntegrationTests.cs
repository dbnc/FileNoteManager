using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Implementation;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Tests.Repositories;

/// <summary>
/// Integration tests using a real SQLite database in a temporary file.
/// Each test class gets a fresh database via IDisposable.
/// </summary>
public class NoteRepositoryIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DatabaseManager _dbManager;
    private readonly NoteRepository _repository;

    public NoteRepositoryIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"fnm_test_{Guid.NewGuid():N}.db");
        _dbManager = new DatabaseManager(_dbPath);
        _repository = new NoteRepository(_dbManager);
    }

    public void Dispose()
    {
        _dbManager.Dispose();
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best-effort cleanup */ }
        }
    }

    // ── Save / GetById ─────────────────────────────────────────

    [Fact]
    public void Save_NewNote_CanBeRetrievedById()
    {
        var note = MakeNote("C:\\test.txt", "Hello SQLite");
        _repository.Save(note);

        var retrieved = _repository.GetById("C:\\test.txt");

        Assert.NotNull(retrieved);
        Assert.Equal("Hello SQLite", retrieved.Content);
        Assert.False(retrieved.IsDeleted);
    }

    [Fact]
    public void Save_ExistingNote_UpdatesContent()
    {
        _repository.Save(MakeNote("C:\\update.txt", "Original"));
        _repository.Save(MakeNote("C:\\update.txt", "Updated"));

        var result = _repository.GetById("C:\\update.txt");

        Assert.Equal("Updated", result!.Content);
    }

    [Fact]
    public void GetById_NonExistentPath_ReturnsNull()
    {
        Assert.Null(_repository.GetById("C:\\does_not_exist.txt"));
    }

    // ── GetAll ─────────────────────────────────────────────────

    [Fact]
    public void GetAll_ReturnsOnlyNonDeletedNotes()
    {
        _repository.Save(MakeNote("C:\\a.txt", "Active"));
        _repository.Save(MakeNote("C:\\b.txt", "Also active"));
        _repository.Save(MakeNote("C:\\c.txt", "Deleted", isDeleted: true));

        var all = _repository.GetAll().ToList();

        Assert.Equal(2, all.Count);
        Assert.DoesNotContain(all, n => n.Path == "C:\\c.txt");
    }

    // ── Delete ─────────────────────────────────────────────────

    [Fact]
    public void Delete_ExistingNote_RemovesFromDatabase()
    {
        _repository.Save(MakeNote("C:\\del.txt", "To delete"));
        _repository.Delete("C:\\del.txt");

        Assert.Null(_repository.GetById("C:\\del.txt"));
    }

    // ── UpdatePath ─────────────────────────────────────────────

    [Fact]
    public void UpdatePath_MovesNoteToNewPath()
    {
        _repository.Save(MakeNote("C:\\old.txt", "Moved note"));
        _repository.UpdatePath("C:\\old.txt", "C:\\new.txt");

        Assert.Null(_repository.GetById("C:\\old.txt"));
        Assert.NotNull(_repository.GetById("C:\\new.txt"));
        Assert.Equal("Moved note", _repository.GetById("C:\\new.txt")!.Content);
    }

    // ── Counts ─────────────────────────────────────────────────

    [Fact]
    public void GetCount_ReturnsOnlyActiveNotes()
    {
        _repository.Save(MakeNote("C:\\1.txt", "Note 1"));
        _repository.Save(MakeNote("C:\\2.txt", "Note 2"));
        _repository.Save(MakeNote("C:\\3.txt", "Deleted", isDeleted: true));

        Assert.Equal(2, _repository.GetCount());
    }

    [Fact]
    public void GetFileCount_CountsOnlyFileNotes()
    {
        _repository.Save(MakeNote("C:\\file.txt", "File note", isFolder: false));
        _repository.Save(MakeNote("C:\\folder", "Folder note", isFolder: true));

        Assert.Equal(1, _repository.GetFileCount());
    }

    [Fact]
    public void GetFolderCount_CountsOnlyFolderNotes()
    {
        _repository.Save(MakeNote("C:\\file.txt", "File note", isFolder: false));
        _repository.Save(MakeNote("C:\\folder", "Folder note", isFolder: true));

        Assert.Equal(1, _repository.GetFolderCount());
    }

    // ── Helpers ────────────────────────────────────────────────

    private static NoteEntity MakeNote(string path, string content,
        bool isDeleted = false, bool isFolder = false) => new()
    {
        Path = path,
        Content = content,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDeleted = isDeleted,
        IsFolder = isFolder
    };
}
