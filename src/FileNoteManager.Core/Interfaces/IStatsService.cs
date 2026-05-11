using FileNoteManager.Core.Models;

namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// Service for providing statistics
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// Get summary statistics
    /// </summary>
    StatsSummary GetSummary();
    
    /// <summary>
    /// Get daily statistics for the last N days
    /// </summary>
    IEnumerable<DailyStats> GetTrend(int days = 7);
    
    /// <summary>
    /// Get the top N most used tags
    /// </summary>
    IEnumerable<TagStats> GetTopTags(int count = 10);
}
