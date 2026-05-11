using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Implementation of backup service
/// </summary>
public class BackupService : IBackupService
{
    private readonly IBackupRepository _backupRepository;
    private readonly string _databasePath;
    private readonly string _backupDirectory;
    private readonly int _maxBackups;
    
    public BackupService(
        IBackupRepository backupRepository,
        string databasePath,
        string? backupDirectory = null,
        int maxBackups = 5)
    {
        _backupRepository = backupRepository;
        _databasePath = databasePath;
        _backupDirectory = backupDirectory ?? 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FileNoteManager",
                "backups"
            );
        _maxBackups = maxBackups;
    }
    
    public void CreateBackup(string backupPath)
    {
        // Ensure backup directory exists
        var directory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Copy database file
        File.Copy(_databasePath, backupPath, overwrite: true);
        
        // Record backup
        var fileInfo = new FileInfo(backupPath);
        _backupRepository.Save(new BackupRecordEntity
        {
            BackupPath = backupPath,
            CreatedAt = DateTime.UtcNow,
            FileSize = fileInfo.Length
        });
        
        // Cleanup old backups
        CleanupOldBackups(_maxBackups);
    }
    
    public void RestoreBackup(string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException($"Backup file not found: {backupPath}");
        }
        
        // Copy backup file over current database
        File.Copy(backupPath, _databasePath, overwrite: true);
    }
    
    public DateTime? GetLastBackupTime()
    {
        var record = _backupRepository.GetMostRecent();
        return record?.CreatedAt;
    }
    
    public void CleanupOldBackups(int maxBackups = 5)
    {
        var records = _backupRepository.GetAll().ToList();
        
        while (records.Count > maxBackups)
        {
            var oldest = records.Last();
            
            // Delete backup file
            if (File.Exists(oldest.BackupPath))
            {
                File.Delete(oldest.BackupPath);
            }
            
            // Delete record
            _backupRepository.Delete(oldest.Id);
            records.Remove(oldest);
        }
    }
    
    public bool ShouldAutoBackup()
    {
        var lastBackup = GetLastBackupTime();
        
        if (!lastBackup.HasValue)
        {
            return true;
        }
        
        var daysSinceLastBackup = (DateTime.UtcNow - lastBackup.Value).TotalDays;
        return daysSinceLastBackup >= 7;
    }
    
    /// <summary>
    /// Create an automatic backup with timestamp
    /// </summary>
    public string CreateAutoBackup()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(_backupDirectory, $"fnm_backup_{timestamp}.db");
        
        CreateBackup(backupPath);
        
        return backupPath;
    }
}
