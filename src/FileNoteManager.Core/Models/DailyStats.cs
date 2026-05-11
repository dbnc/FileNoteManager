namespace FileNoteManager.Core.Models;

/// <summary>
/// Statistics for a single day
/// </summary>
public class DailyStats
{
    /// <summary>
    /// The date
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Number of notes created on this date
    /// </summary>
    public int NotesCreated { get; set; }
    
    /// <summary>
    /// Number of notes updated on this date
    /// </summary>
    public int NotesUpdated { get; set; }
}
