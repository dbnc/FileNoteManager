namespace FileNoteManager.Core.Repositories.Interfaces;

/// <summary>
/// Repository interface for note entities
/// </summary>
public interface INoteRepository
{
    /// <summary>
    /// Get a note by its path
    /// </summary>
    NoteEntity? GetById(string path);
    
    /// <summary>
    /// Get all notes
    /// </summary>
    IEnumerable<NoteEntity> GetAll();
    
    /// <summary>
    /// Save a note (insert or update)
    /// </summary>
    void Save(NoteEntity note);
    
    /// <summary>
    /// Delete a note by path
    /// </summary>
    void Delete(string path);
    
    /// <summary>
    /// Search notes by keyword
    /// </summary>
    IEnumerable<NoteEntity> Search(string keyword);
    
    /// <summary>
    /// Update the path of a note
    /// </summary>
    void UpdatePath(string oldPath, string newPath);
    
    /// <summary>
    /// Get count of notes
    /// </summary>
    int GetCount();
    
    /// <summary>
    /// Get count of file notes
    /// </summary>
    int GetFileCount();
    
    /// <summary>
    /// Get count of folder notes
    /// </summary>
    int GetFolderCount();

    /// <summary>
    /// Find soft-deleted notes whose path ends with the given file/folder name.
    /// Used for migrating notes when a file is moved or renamed across directories.
    /// </summary>
    IEnumerable<NoteEntity> FindDeletedByFileName(string fileName);

    /// <summary>
    /// Find active (non-deleted) notes whose path ends with the given file/folder name.
    /// Used for migrating notes when a file was moved while the app was not running.
    /// </summary>
    IEnumerable<NoteEntity> FindActiveByFileName(string fileName);
}
