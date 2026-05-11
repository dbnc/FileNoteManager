using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of tag service
/// </summary>
public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly INoteTagRepository _noteTagRepository;
    private readonly INoteRepository _noteRepository;
    
    public TagService(
        ITagRepository tagRepository,
        INoteTagRepository noteTagRepository,
        INoteRepository noteRepository)
    {
        _tagRepository = tagRepository;
        _noteTagRepository = noteTagRepository;
        _noteRepository = noteRepository;
    }
    
    public IEnumerable<Tag> GetAllTags()
    {
        var entities = _tagRepository.GetAll();
        return entities.Select(MapToModel);
    }
    
    public Tag? GetTagByName(string name)
    {
        var entity = _tagRepository.GetByName(name);
        return entity != null ? MapToModel(entity) : null;
    }
    
    public IEnumerable<Tag> GetMostUsedTags(int count)
    {
        var entities = _tagRepository.GetMostUsed(count);
        return entities.Select(MapToModel);
    }
    
    public IEnumerable<Note> GetNotesByTag(string tagName)
    {
        var notePaths = _noteTagRepository.GetNotesForTag(tagName);
        
        return notePaths
            .Select(path => _noteRepository.GetById(path))
            .Where(entity => entity != null && !entity.IsDeleted)
            .Select(entity => MapNoteEntityToModel(entity!));
    }
    
    public Dictionary<string, int> GetTagFrequency()
    {
        return _noteTagRepository.GetTagFrequency();
    }
    
    /// <summary>
    /// Parse a comma or space-separated tag string into individual tags
    /// </summary>
    public static List<string> ParseTags(string? tagInput)
    {
        if (string.IsNullOrWhiteSpace(tagInput))
        {
            return new List<string>();
        }
        
        // Split by comma or space
        var tags = tagInput.Split(new[] { ',', ' ', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .ToList();
        
        return tags;
    }
    
    private Tag MapToModel(TagEntity entity)
    {
        return new Tag
        {
            Name = entity.Name,
            CreatedAt = entity.CreatedAt,
            UsageCount = entity.UsageCount
        };
    }
    
    private Note MapNoteEntityToModel(Repositories.NoteEntity entity)
    {
        return new Note
        {
            Path = entity.Path,
            Content = entity.Content,
            Tags = _noteTagRepository.GetTagsForNote(entity.Path).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            IsFolder = entity.IsFolder
        };
    }
}
