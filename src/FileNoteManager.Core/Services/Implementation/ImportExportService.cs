using System.Text.Json;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;
using CsvHelper;
using System.Globalization;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of import/export service
/// </summary>
public class ImportExportService : IImportExportService
{
    private readonly INoteRepository _noteRepository;
    private readonly ITagRepository _tagRepository;
    private readonly INoteTagRepository _noteTagRepository;
    
    public ImportExportService(
        INoteRepository noteRepository,
        ITagRepository tagRepository,
        INoteTagRepository noteTagRepository)
    {
        _noteRepository = noteRepository;
        _tagRepository = tagRepository;
        _noteTagRepository = noteTagRepository;
    }
    
    public void ExportToJson(string filePath)
    {
        var notes = _noteRepository.GetAll();
        var exportData = new List<NoteExportData>();
        
        foreach (var note in notes)
        {
            var tags = _noteTagRepository.GetTagsForNote(note.Path).ToList();
            exportData.Add(new NoteExportData
            {
                Path = note.Path,
                Content = note.Content,
                Tags = tags,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                IsFolder = note.IsFolder
            });
        }
        
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText(filePath, json);
    }
    
    public void ExportToCsv(string filePath)
    {
        var notes = _noteRepository.GetAll();
        
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        csv.WriteField("Path");
        csv.WriteField("Content");
        csv.WriteField("Tags");
        csv.WriteField("CreatedAt");
        csv.WriteField("UpdatedAt");
        csv.WriteField("IsFolder");
        csv.NextRecord();
        
        foreach (var note in notes)
        {
            var tags = string.Join(";", _noteTagRepository.GetTagsForNote(note.Path));
            
            csv.WriteField(note.Path);
            csv.WriteField(note.Content);
            csv.WriteField(tags);
            csv.WriteField(note.CreatedAt.ToString("O"));
            csv.WriteField(note.UpdatedAt.ToString("O"));
            csv.WriteField(note.IsFolder);
            csv.NextRecord();
        }
    }
    
    public ImportResult Import(string filePath, ImportConflictResolution resolution)
    {
        var result = new ImportResult();
        
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            List<NoteExportData> importData;
            
            if (extension == ".json")
            {
                importData = ImportFromJson(filePath);
            }
            else if (extension == ".csv")
            {
                importData = ImportFromCsv(filePath);
            }
            else
            {
                throw new NotSupportedException($"Unsupported file format: {extension}");
            }
            
            foreach (var data in importData)
            {
                try
                {
                    var existing = _noteRepository.GetById(data.Path);
                    
                    if (existing != null)
                    {
                        if (resolution == ImportConflictResolution.Skip)
                        {
                            result.Skipped++;
                            continue;
                        }
                        else if (resolution == ImportConflictResolution.Ask)
                        {
                            // Return with partial result for user decision
                            // In actual implementation, this would prompt the user
                            result.Skipped++;
                            continue;
                        }
                        // Otherwise overwrite
                    }
                    
                    var note = new NoteEntity
                    {
                        Path = data.Path,
                        Content = data.Content,
                        CreatedAt = data.CreatedAt,
                        UpdatedAt = data.UpdatedAt,
                        IsDeleted = false,
                        IsFolder = data.IsFolder
                    };
                    
                    _noteRepository.Save(note);
                    
                    // Handle tags
                    foreach (var tag in data.Tags)
                    {
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
                        
                        _noteTagRepository.AddTagToNote(data.Path, tag);
                    }
                    
                    result.Imported++;
                }
                catch
                {
                    result.Failed++;
                }
            }
        }
        catch
        {
            throw;
        }
        
        return result;
    }
    
    private List<NoteExportData> ImportFromJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<NoteExportData>>(json) ?? new List<NoteExportData>();
    }
    
    private List<NoteExportData> ImportFromCsv(string filePath)
    {
        var results = new List<NoteExportData>();
        
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Read();
        csv.ReadHeader();
        
        while (csv.Read())
        {
            var data = new NoteExportData
            {
                Path = csv.GetField("Path"),
                Content = csv.GetField("Content"),
                Tags = csv.GetField("Tags").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                CreatedAt = DateTime.Parse(csv.GetField("CreatedAt")),
                UpdatedAt = DateTime.Parse(csv.GetField("UpdatedAt")),
                IsFolder = csv.GetField("IsFolder") == "True"
            };
            
            results.Add(data);
        }
        
        return results;
    }
    
    /// <summary>
    /// Data structure for export/import
    /// </summary>
    private class NoteExportData
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsFolder { get; set; }
    }
}
