using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of note service
/// </summary>
public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly ITagRepository _tagRepository;
    private readonly INoteTagRepository _noteTagRepository;
    private readonly IHistoryRepository _historyRepository;
    
    public NoteService(
        INoteRepository noteRepository,
        ITagRepository tagRepository,
        INoteTagRepository noteTagRepository,
        IHistoryRepository historyRepository)
    {
        _noteRepository = noteRepository;
        _tagRepository = tagRepository;
        _noteTagRepository = noteTagRepository;
        _historyRepository = historyRepository;
    }
    
    public Note? GetNote(string path)
    {
        var entity = _noteRepository.GetById(path);
        if (entity == null || entity.IsDeleted)
        {
            return null;
        }
        
        return MapToModel(entity);
    }
    
    public Note CreateNote(string path, string content, IEnumerable<string> tags)
    {
        // If content is blank, treat as delete
        if (string.IsNullOrWhiteSpace(content))
        {
            var existing = _noteRepository.GetById(path);
            if (existing != null)
            {
                _noteRepository.Delete(path);
                _noteTagRepository.RemoveAllTagsFromNote(path);
            }
            throw new ArgumentException("Note content cannot be blank");
        }
        
        var now = DateTime.UtcNow;
        var isFolder = Directory.Exists(path);
        
        var entity = new NoteEntity
        {
            Path = path,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            IsFolder = isFolder
        };
        
        _noteRepository.Save(entity);
        
        // Handle tags
        var tagList = tags.ToList();
        SetTagsForNote(path, tagList);

        // Write Desktop.ini InfoTip for folder hover tooltip
        if (isFolder) UpdateFolderDesktopIni(path, content);

        // Write NTFS ADS so the note travels with the file on move/copy
        if (!isFolder) WriteAds(path, content);
        
        return MapToModel(entity, tagList);
    }
    
    public Note UpdateNote(string path, string content, IEnumerable<string> tags)
    {
        var existing = _noteRepository.GetById(path);
        
        if (existing == null)
        {
            return CreateNote(path, content, tags);
        }
        
        // If content is blank, delete the note
        if (string.IsNullOrWhiteSpace(content))
        {
            _noteRepository.Delete(path);
            _noteTagRepository.RemoveAllTagsFromNote(path);
            throw new ArgumentException("Note content cannot be blank");
        }
        
        // Save history before updating
        _historyRepository.Save(new HistoryEntity
        {
            NotePath = path,
            Content = existing.Content,
            SavedAt = DateTime.UtcNow,
            VersionNumber = _historyRepository.GetNextVersionNumber(path)
        });
        
        // Cleanup old versions
        _historyRepository.DeleteOldVersions(path, 10);
        
        existing.Content = content;
        existing.UpdatedAt = DateTime.UtcNow;
        
        _noteRepository.Save(existing);
        
        // Handle tags
        var tagList = tags.ToList();
        SetTagsForNote(path, tagList);

        // Write Desktop.ini InfoTip for folder hover tooltip
        if (existing.IsFolder) UpdateFolderDesktopIni(path, content);

        // Write NTFS ADS so the note travels with the file on move/copy
        if (!existing.IsFolder) WriteAds(path, content);

        return MapToModel(existing, tagList);
    }
    
    public void DeleteNote(string path)
    {
        var existing = _noteRepository.GetById(path);
        if (existing != null)
        {
            _noteRepository.Delete(path);
            _noteTagRepository.RemoveAllTagsFromNote(path);
            if (existing.IsFolder) UpdateFolderDesktopIni(path, null);
            else ClearAds(path);
        }
    }
    
    public IEnumerable<Note> SearchNotes(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Enumerable.Empty<Note>();
        }
        
        var entities = _noteRepository.Search(keyword);
        return entities.Select(e => MapToModel(e));
    }
    
    public IEnumerable<Note> SearchByTags(IEnumerable<string> tags)
    {
        var tagList = tags.ToList();
        if (!tagList.Any())
        {
            return Enumerable.Empty<Note>();
        }
        
        var allNotes = new HashSet<string>();
        
        foreach (var tag in tagList)
        {
            var notePaths = _noteTagRepository.GetNotesForTag(tag);
            foreach (var path in notePaths)
            {
                allNotes.Add(path);
            }
        }
        
        return allNotes.Select(path => GetNote(path))
                       .Where(n => n != null)
                       .Cast<Note>();
    }
    
    public void UpdatePath(string oldPath, string newPath)
    {
        _noteRepository.UpdatePath(oldPath, newPath);

        // If this was a folder note, the folder was renamed/moved.
        // Desktop.ini travels with the folder, so re-write InfoTip at the new location.
        var updated = _noteRepository.GetById(newPath);
        if (updated != null && updated.IsFolder)
            UpdateFolderDesktopIni(newPath, updated.Content);
    }
    
    public void MarkAsDeleted(string path)
    {
        var existing = _noteRepository.GetById(path);
        if (existing != null)
        {
            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;
            _noteRepository.Save(existing);
        }
    }
    
    public bool HasNote(string path)
    {
        var note = _noteRepository.GetById(path);
        return note != null && !note.IsDeleted;
    }

    public Note? TryMigrateNote(string newPath)
    {
        // Don't overwrite an active note that already exists at newPath
        var existing = _noteRepository.GetById(newPath);
        if (existing != null && !existing.IsDeleted) return null;

        var fileName = Path.GetFileName(newPath.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(fileName)) return null;

        // ── Strategy 1: app was RUNNING during move ───────────────────────
        // The FileSystemWatcher fired and soft-deleted the note at the old path.
        var deletedCandidates = _noteRepository.FindDeletedByFileName(fileName).ToList();
        if (deletedCandidates.Count == 1)
            return ApplyMigration(deletedCandidates[0], newPath, wasDeleted: true);

        // ── Strategy 2: app was CLOSED during move ─────────────────────────
        // The note at the old path is still ACTIVE but the file/folder no longer
        // exists there.  Filter candidates to those whose old path is truly gone.
        var orphans = _noteRepository.FindActiveByFileName(fileName)
            .Where(e => !File.Exists(e.Path) && !Directory.Exists(e.Path))
            .ToList();

        if (orphans.Count == 1)
            return ApplyMigration(orphans[0], newPath, wasDeleted: false);

        // ── Strategy 3: NTFS Alternate Data Stream ─────────────────────────
        // When a file is moved or copied on Windows, its ADS travels with it.
        // Check for a :fnm stream at newPath — works for both moves AND copies.
        if (!Directory.Exists(newPath))
        {
            var adsContent = ReadAds(newPath);
            if (!string.IsNullOrWhiteSpace(adsContent))
            {
                try { return CreateNote(newPath, adsContent, Enumerable.Empty<string>()); }
                catch { }
            }
        }

        return null;
    }

    // ── NTFS ADS helpers ─────────────────────────────────────────────────
    // Stream name :fnm  (FileNoteManager)
    private const string AdsStreamSuffix = ":fnm";

    private static void WriteAds(string filePath, string content)
    {
        try { File.WriteAllText(filePath + AdsStreamSuffix, content); }
        catch { /* ADS not supported (FAT/network share) — ignore */ }
    }

    private static string? ReadAds(string filePath)
    {
        try { return File.ReadAllText(filePath + AdsStreamSuffix); }
        catch { return null; }
    }

    private static void ClearAds(string filePath)
    {
        try { File.Delete(filePath + AdsStreamSuffix); }
        catch { }
    }

    private Note? ApplyMigration(NoteEntity candidate, string newPath, bool wasDeleted)
    {
        _noteRepository.UpdatePath(candidate.Path, newPath);

        var migrated = _noteRepository.GetById(newPath);
        if (migrated == null) return null;

        if (wasDeleted)
        {
            migrated.IsDeleted = false;
            migrated.UpdatedAt = DateTime.UtcNow;
            _noteRepository.Save(migrated);
        }

        if (migrated.IsFolder) UpdateFolderDesktopIni(newPath, migrated.Content);

        return MapToModel(migrated);
    }
    
    private void SetTagsForNote(string path, List<string> tags)
    {
        // Remove all existing tags
        _noteTagRepository.RemoveAllTagsFromNote(path);
        
        // Add new tags
        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            // Ensure tag exists
            var tagEntity = _tagRepository.GetByName(tag);
            if (tagEntity == null)
            {
                tagEntity = new TagEntity
                {
                    Name = tag,
                    CreatedAt = DateTime.UtcNow,
                    UsageCount = 0
                };
                _tagRepository.Save(tagEntity);
            }
            
            _noteTagRepository.AddTagToNote(path, tag);
        }
    }
    
    /// <summary>
    /// Write/clear the [.ShellClassInfo] InfoTip entry in Desktop.ini for folder notes.
    /// Windows Explorer reads this value and shows it as the hover tooltip for the folder.
    /// </summary>
    private static void UpdateFolderDesktopIni(string folderPath, string? content)
    {
        if (!Directory.Exists(folderPath)) return;
        try
        {
            var iniPath = Path.Combine(folderPath, "desktop.ini");
            List<string> lines = File.Exists(iniPath) ? File.ReadAllLines(iniPath).ToList() : new();

            const string section = "[.ShellClassInfo]";
            const string key = "InfoTip=";

            int sectionIdx = lines.FindIndex(l => l.TrimStart().StartsWith(section, StringComparison.OrdinalIgnoreCase));
            if (sectionIdx < 0)
            {
                if (string.IsNullOrEmpty(content)) return;
                lines.Add(section);
                lines.Add(key + content);
            }
            else
            {
                // find existing InfoTip= line inside the section
                int tipIdx = -1;
                for (int i = sectionIdx + 1; i < lines.Count; i++)
                {
                    var trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith("[")) break; // next section
                    if (trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase)) { tipIdx = i; break; }
                }

                if (string.IsNullOrEmpty(content))
                {
                    if (tipIdx >= 0) lines.RemoveAt(tipIdx);
                }
                else if (tipIdx >= 0)
                {
                    lines[tipIdx] = key + content;
                }
                else
                {
                    lines.Insert(sectionIdx + 1, key + content);
                }
            }

            // Trim blank InfoTip lines
            File.WriteAllLines(iniPath, lines);

            // Set Hidden+System on desktop.ini so Explorer picks it up
            var attr = File.GetAttributes(iniPath);
            File.SetAttributes(iniPath, attr | FileAttributes.Hidden | FileAttributes.System);

            // Also mark the folder as System so Explorer reads Desktop.ini
            var folderAttr = File.GetAttributes(folderPath);
            if ((folderAttr & FileAttributes.System) == 0)
                File.SetAttributes(folderPath, folderAttr | FileAttributes.System);
        }
        catch { /* non-critical */ }
    }

    private Note MapToModel(NoteEntity entity, List<string>? tags = null)
    {
        return new Note
        {
            Path = entity.Path,
            Content = entity.Content,
            Tags = tags ?? _noteTagRepository.GetTagsForNote(entity.Path).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            IsFolder = entity.IsFolder
        };
    }
}
