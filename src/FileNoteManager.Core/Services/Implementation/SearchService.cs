using Dapper;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories.Interfaces;
using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of search service using FTS5
/// </summary>
public class SearchService : ISearchService
{
    private readonly DatabaseManager _dbManager;
    private readonly INoteTagRepository _noteTagRepository;
    
    public SearchService(DatabaseManager dbManager, INoteTagRepository noteTagRepository)
    {
        _dbManager = dbManager;
        _noteTagRepository = noteTagRepository;
    }
    
    public IEnumerable<SearchResult> Search(string keyword, SearchOptions? options = null)
    {
        options ??= new SearchOptions();
        
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Enumerable.Empty<SearchResult>();
        }
        
        using var connection = _dbManager.CreateConnection();
        
        // Build the FTS query
        var ftsQuery = BuildFtsQuery(keyword, options);
        
        var results = connection.Query<dynamic>(
            ftsQuery.Sql,
            new { Keyword = keyword, Limit = options.MaxResults }
        );
        
        return results.Select(r => new SearchResult
        {
            Path = r.Path,
            Content = r.Content ?? string.Empty,
            Tags = _noteTagRepository.GetTagsForNote(r.Path).ToList(),
            UpdatedAt = ParseDateTime(r.UpdatedAt),
            IsFolder = r.IsFolder == 1,
            Snippet = GenerateSnippet(r.Content?.ToString() ?? string.Empty, keyword)
        });
    }
    
    public IEnumerable<SearchResult> SearchByContent(string content)
    {
        return Search(content, new SearchOptions { IncludeContent = true, IncludeTags = false, IncludePath = false });
    }
    
    public IEnumerable<SearchResult> SearchByPath(string pathPattern)
    {
        using var connection = _dbManager.CreateConnection();
        
        var results = connection.Query<dynamic>(
            @"SELECT n.Path, n.Content, n.UpdatedAt, n.IsFolder
              FROM NoteEntity n
              WHERE n.Path LIKE @PathPattern AND n.IsDeleted = 0
              ORDER BY n.UpdatedAt DESC",
            new { PathPattern = $"%{pathPattern}%" }
        );
        
        return results.Select(r => new SearchResult
        {
            Path = r.Path,
            Content = r.Content ?? string.Empty,
            Tags = _noteTagRepository.GetTagsForNote(r.Path).ToList(),
            UpdatedAt = ParseDateTime(r.UpdatedAt),
            IsFolder = r.IsFolder == 1
        });
    }
    
    public IEnumerable<SearchResult> SearchByDateRange(DateTime from, DateTime to)
    {
        using var connection = _dbManager.CreateConnection();
        
        var results = connection.Query<dynamic>(
            @"SELECT n.Path, n.Content, n.UpdatedAt, n.IsFolder
              FROM NoteEntity n
              WHERE n.UpdatedAt >= @From AND n.UpdatedAt <= @To AND n.IsDeleted = 0
              ORDER BY n.UpdatedAt DESC",
            new { From = from, To = to }
        );
        
        return results.Select(r => new SearchResult
        {
            Path = r.Path,
            Content = r.Content ?? string.Empty,
            Tags = _noteTagRepository.GetTagsForNote(r.Path).ToList(),
            UpdatedAt = ParseDateTime(r.UpdatedAt),
            IsFolder = r.IsFolder == 1
        });
    }

    private static DateTime ParseDateTime(dynamic value)
    {
        if (value is DateTime dt) return dt;
        if (value is DateTimeOffset dto) return dto.UtcDateTime;
        var str = value?.ToString() ?? string.Empty;
        return DateTime.TryParse(str, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime result)
            ? result
            : DateTime.UtcNow;
    }

    private (string Sql, object Parameters) BuildFtsQuery(string keyword, SearchOptions options)
    {
        var selectFields = "n.Path, n.Content, n.UpdatedAt, n.IsFolder";
        var fromClause = "FROM NoteEntity n";
        var whereClause = "WHERE n.IsDeleted = 0";
        var orderClause = "ORDER BY n.UpdatedAt DESC";
        var limitClause = "LIMIT @Limit";
        
        if (options.IncludeContent || options.IncludeTags)
        {
            fromClause += " INNER JOIN NoteFTS fts ON n.Path = fts.Path";
            whereClause += " AND NoteFTS MATCH @Keyword";
        }
        
        if (options.IncludePath && !options.IncludeContent)
        {
            whereClause += " AND n.Path LIKE '%' || @Keyword || '%'";
        }
        
        var sql = $@"
            SELECT {selectFields}
            {fromClause}
            {whereClause}
            {orderClause}
            {limitClause}";
        
        return (sql, new { Keyword = keyword, Limit = options.MaxResults });
    }
    
    private string? GenerateSnippet(string content, string keyword)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(keyword))
        {
            return null;
        }
        
        var index = content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
        }
        
        var start = Math.Max(0, index - 30);
        var end = Math.Min(content.Length, index + keyword.Length + 50);
        var snippet = content.Substring(start, end - start);
        
        if (start > 0) snippet = "..." + snippet;
        if (end < content.Length) snippet = snippet + "...";
        
        return snippet;
    }
}
