using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for searching notes
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Search notes by keyword with options
    /// </summary>
    IEnumerable<SearchResult> Search(string keyword, SearchOptions? options = null);
    
    /// <summary>
    /// Search notes by content only
    /// </summary>
    IEnumerable<SearchResult> SearchByContent(string content);
    
    /// <summary>
    /// Search notes by path pattern
    /// </summary>
    IEnumerable<SearchResult> SearchByPath(string pathPattern);
    
    /// <summary>
    /// Search notes by date range
    /// </summary>
    IEnumerable<SearchResult> SearchByDateRange(DateTime from, DateTime to);
}
