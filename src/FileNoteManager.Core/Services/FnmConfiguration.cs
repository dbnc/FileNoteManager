namespace FileNoteManager.Core.Services;

/// <summary>
/// Configuration for the File Note Manager application
/// </summary>
public class FnmConfiguration
{
    /// <summary>
    /// Database configuration
    /// </summary>
    public DatabaseConfig Database { get; set; } = new();
    
    /// <summary>
    /// UI configuration
    /// </summary>
    public UiConfig Ui { get; set; } = new();
    
    /// <summary>
    /// Shortcut configuration
    /// </summary>
    public ShortcutConfig Shortcuts { get; set; } = new();
    
    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
}

/// <summary>
/// Database configuration
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Path to the database file
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Interval in days between automatic backups
    /// </summary>
    public int BackupIntervalDays { get; set; } = 7;
    
    /// <summary>
    /// Maximum number of backup files to keep
    /// </summary>
    public int MaxBackupCount { get; set; } = 5;
    
    public DatabaseConfig()
    {
        Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileNoteManager",
            "fnm.db"
        );
    }
}

/// <summary>
/// UI configuration
/// </summary>
public class UiConfig
{
    /// <summary>
    /// Current theme (light or dark)
    /// </summary>
    public string Theme { get; set; } = "light";
    
    /// <summary>
    /// Maximum characters to show in tooltip preview
    /// </summary>
    public int TooltipPreviewLength { get; set; } = 100;
    
    /// <summary>
    /// Maximum search results to return
    /// </summary>
    public int MaxSearchResults { get; set; } = 100;
}

/// <summary>
/// Shortcut configuration
/// </summary>
public class ShortcutConfig
{
    /// <summary>
    /// Shortcut for adding a note
    /// </summary>
    public string AddNote { get; set; } = "Ctrl+Shift+N";
    
    /// <summary>
    /// Shortcut for opening search
    /// </summary>
    public string Search { get; set; } = "Ctrl+Shift+F";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Log level (Debug, Information, Warning, Error)
    /// </summary>
    public string Level { get; set; } = "Information";
    
    /// <summary>
    /// Path to the log file
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Maximum log file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;
    
    /// <summary>
    /// Maximum number of log files to keep
    /// </summary>
    public int MaxFileCount { get; set; } = 5;
    
    public LoggingConfig()
    {
        Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileNoteManager",
            "logs",
            "fnm.log"
        );
    }
}
