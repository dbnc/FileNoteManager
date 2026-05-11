using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of history service
/// </summary>
public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepository;
    private readonly INoteRepository _noteRepository;
    
    public HistoryService(IHistoryRepository historyRepository, INoteRepository noteRepository)
    {
        _historyRepository = historyRepository;
        _noteRepository = noteRepository;
    }
    
    public IEnumerable<NoteHistory> GetHistory(string path)
    {
        var entities = _historyRepository.GetByPath(path);
        return entities.Select(MapToModel);
    }
    
    public NoteHistory? GetVersion(string path, int versionId)
    {
        var entity = _historyRepository.GetById(versionId);
        
        if (entity == null || entity.NotePath != path)
        {
            return null;
        }
        
        return MapToModel(entity);
    }
    
    public void SaveHistory(string path, string content)
    {
        var versionNumber = _historyRepository.GetNextVersionNumber(path);
        
        var entity = new HistoryEntity
        {
            NotePath = path,
            Content = content,
            SavedAt = DateTime.UtcNow,
            VersionNumber = versionNumber
        };
        
        _historyRepository.Save(entity);
        
        // Cleanup old versions
        _historyRepository.DeleteOldVersions(path, 10);
    }
    
    public void RestoreVersion(string path, int versionId)
    {
        var historyEntry = _historyRepository.GetById(versionId);
        if (historyEntry == null || historyEntry.NotePath != path)
        {
            throw new InvalidOperationException($"History version {versionId} not found for path: {path}");
        }

        var currentNote = _noteRepository.GetById(path);
        if (currentNote == null)
        {
            throw new InvalidOperationException($"Note not found for path: {path}");
        }

        // Save current content as a new history entry before restoring
        _historyRepository.Save(new HistoryEntity
        {
            NotePath = path,
            Content = currentNote.Content,
            SavedAt = DateTime.UtcNow,
            VersionNumber = _historyRepository.GetNextVersionNumber(path)
        });

        // Restore the note to the historical content
        currentNote.Content = historyEntry.Content;
        currentNote.UpdatedAt = DateTime.UtcNow;
        _noteRepository.Save(currentNote);

        // Cleanup old versions
        _historyRepository.DeleteOldVersions(path, 10);
    }
    
    public void CleanupOldVersions(string path, int maxVersions = 10)
    {
        _historyRepository.DeleteOldVersions(path, maxVersions);
    }
    
    private NoteHistory MapToModel(HistoryEntity entity)
    {
        return new NoteHistory
        {
            Id = entity.Id,
            NotePath = entity.NotePath,
            Content = entity.Content,
            SavedAt = entity.SavedAt,
            VersionNumber = entity.VersionNumber
        };
    }
}
