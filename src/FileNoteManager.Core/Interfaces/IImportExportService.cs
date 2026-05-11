namespace FileNoteManager.Core.Interfaces;

/// <summary>
/// How to handle import conflicts
/// </summary>
public enum ImportConflictResolution
{
    Skip,
    Overwrite,
    Ask
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Number of notes successfully imported
    /// </summary>
    public int Imported { get; set; }
    
    /// <summary>
    /// Number of notes skipped
    /// </summary>
    public int Skipped { get; set; }
    
    /// <summary>
    /// Number of notes that failed to import
    /// </summary>
    public int Failed { get; set; }
}

/// <summary>
/// Service for importing and exporting notes
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// Export all notes to JSON format
    /// </summary>
    void ExportToJson(string filePath);
    
    /// <summary>
    /// Export all notes to CSV format
    /// </summary>
    void ExportToCsv(string filePath);
    
    /// <summary>
    /// Import notes from a file
    /// </summary>
    ImportResult Import(string filePath, ImportConflictResolution resolution);
}
