using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of statistics service
/// </summary>
public class StatsService : IStatsService
{
    private readonly DatabaseManager _dbManager;
    private readonly INoteRepository _noteRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IBackupRepository _backupRepository;
    private readonly string _databasePath;
    
    public StatsService(
        DatabaseManager dbManager,
        INoteRepository noteRepository,
        ITagRepository tagRepository,
        IBackupRepository backupRepository,
        string databasePath)
    {
        _dbManager = dbManager;
        _noteRepository = noteRepository;
        _tagRepository = tagRepository;
        _backupRepository = backupRepository;
        _databasePath = databasePath;
    }
    
    public StatsSummary GetSummary()
    {
        var fileInfo = new FileInfo(_databasePath);
        
        return new StatsSummary
        {
            TotalNotes = _noteRepository.GetCount(),
            FileNotes = _noteRepository.GetFileCount(),
            FolderNotes = _noteRepository.GetFolderCount(),
            DatabaseSize = fileInfo.Exists ? fileInfo.Length : 0,
            LastBackupTime = _backupRepository.GetMostRecent()?.CreatedAt
        };
    }
    
    public IEnumerable<DailyStats> GetTrend(int days = 7)
    {
        using var connection = _dbManager.CreateConnection();
        
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        
        var results = connection.Query<DailyStats>(
            @"SELECT 
                DATE(CreatedAt) as Date,
                COUNT(*) as NotesCreated,
                0 as NotesUpdated
              FROM NoteEntity
              WHERE CreatedAt >= @StartDate
              GROUP BY DATE(CreatedAt)
              ORDER BY Date DESC",
            new { StartDate = startDate }
        );
        
        // Fill in missing dates
        var statsByDate = results.ToDictionary(s => s.Date.Date);
        var allDates = Enumerable.Range(0, days)
            .Select(offset => DateTime.UtcNow.Date.AddDays(-offset))
            .ToList();
        
        return allDates.Select(date => statsByDate.GetValueOrDefault(date.Date, new DailyStats
        {
            Date = date,
            NotesCreated = 0,
            NotesUpdated = 0
        }));
    }
    
    public IEnumerable<TagStats> GetTopTags(int count = 10)
    {
        var tags = _tagRepository.GetMostUsed(count);
        
        return tags.Select(t => new TagStats
        {
            Name = t.Name,
            UsageCount = t.UsageCount
        });
    }
}
