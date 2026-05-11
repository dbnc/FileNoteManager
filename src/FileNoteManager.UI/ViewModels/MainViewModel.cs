using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Shell;
using Microsoft.Win32;

namespace FileNoteManager.UI.ViewModels;

/// <summary>
/// Main window ViewModel. Manages note list, editor, search, stats, and shell registration.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INoteService _noteService;
    private readonly ISearchService _searchService;
    private readonly INoteRepository _noteRepository;
    private readonly IFileWatcherService _fileWatcher;
    private readonly ITagService _tagService;
    private readonly IStatsService _statsService;
    private readonly IBackupService _backupService;
    private readonly IImportExportService _importExportService;

    private List<NoteListItemViewModel> _allNotes = new();

    public MainViewModel(
        INoteService noteService,
        ISearchService searchService,
        INoteRepository noteRepository,
        IFileWatcherService fileWatcher,
        ITagService tagService,
        IStatsService statsService,
        IBackupService backupService,
        IImportExportService importExportService)
    {
        _noteService = noteService;
        _searchService = searchService;
        _noteRepository = noteRepository;
        _fileWatcher = fileWatcher;
        _tagService = tagService;
        _statsService = statsService;
        _backupService = backupService;
        _importExportService = importExportService;

        Notes = new ObservableCollection<NoteListItemViewModel>();
        EditorTags = new ObservableCollection<string>();

        LoadNotes();
        try { RefreshStats(); } catch { }

        // Subscribe to file system changes and start watching
        _fileWatcher.FileChanged += OnFileSystemChanged;
        _fileWatcher.Start();
    }

    // ──────────────────────────────────────────────────
    // Observable Properties
    // ──────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<NoteListItemViewModel> _notes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorEnabled))]
    private NoteListItemViewModel? _selectedNote;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _editorPath = string.Empty;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _editorTags;

    [ObservableProperty]
    private string _tagInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _totalNotes;

    [ObservableProperty]
    private string _databaseSizeDisplay = string.Empty;

    [ObservableProperty]
    private string _lastBackupDisplay = "从未";

    [ObservableProperty]
    private bool _isShellRegistered;

    public bool IsEditorEnabled => SelectedNote != null;

    // ──────────────────────────────────────────────────
    // Property Change Callbacks
    // ──────────────────────────────────────────────────

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter(value);
    }

    partial void OnSelectedNoteChanged(NoteListItemViewModel? value)
    {
        if (value == null)
        {
            EditorPath = string.Empty;
            EditorContent = string.Empty;
            EditorTags.Clear();
            return;
        }

        EditorPath = value.Path;
        var note = _noteService.GetNote(value.Path);

        if (note == null)
        {
            // Path may be stale (file moved while app was closed) — attempt migration
            note = _noteService.TryMigrateNote(value.Path);
            if (note != null)
            {
                EditorPath = note.Path;
                LoadNotes();   // rebuilds list with updated path
                SetStatus($"已自动迁移备注: {System.IO.Path.GetFileName(note.Path.TrimEnd('\\', '/'))}");
            }
        }

        EditorContent = note?.Content ?? string.Empty;
        EditorTags.Clear();
        foreach (var tag in note?.Tags ?? new List<string>())
            EditorTags.Add(tag);
    }

    // ──────────────────────────────────────────────────
    // Commands
    // ──────────────────────────────────────────────────

    [RelayCommand]
    private void SaveNote()
    {
        if (string.IsNullOrWhiteSpace(EditorPath)) return;

        try
        {
            _noteService.UpdateNote(EditorPath, EditorContent, EditorTags);
            RefreshNoteInList(EditorPath);
            RefreshStats();
            SetStatus("备注已保存", success: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DeleteNote()
    {
        if (string.IsNullOrWhiteSpace(EditorPath)) return;

        var result = MessageBox.Show(
            $"确认删除此备注？\n{EditorPath}",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _fileWatcher.UnregisterPath(EditorPath);
            _noteService.DeleteNote(EditorPath);
            var item = Notes.FirstOrDefault(n => n.Path == EditorPath);
            if (item != null) Notes.Remove(item);
            _allNotes.RemoveAll(n => n.Path == EditorPath);
            SelectedNote = null;
            RefreshStats();
            SetStatus("备注已删除");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void AddTag()
    {
        var tag = TagInput.Trim();
        if (string.IsNullOrWhiteSpace(tag) || EditorTags.Contains(tag)) return;
        EditorTags.Add(tag);
        TagInput = string.Empty;
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        EditorTags.Remove(tag);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrWhiteSpace(EditorPath)) return;
        try
        {
            if (System.IO.File.Exists(EditorPath))
                // Open Explorer with the file highlighted
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{EditorPath}\"");
            else if (System.IO.Directory.Exists(EditorPath))
                System.Diagnostics.Process.Start("explorer.exe", $"\"{EditorPath}\"");
            else
                SetStatus("路径不存在，文件可能已被移动或删除");
        }
        catch (Exception ex)
        {
            SetStatus($"无法打开: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadNotes();
        try { RefreshStats(); } catch { }
        if (!StatusMessage.StartsWith("❌"))
            SetStatus("已刷新");
    }

    [RelayCommand]
    private void ExportJson()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = "fnm_export.json",
            Title = "导出备注到 JSON"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _importExportService.ExportToJson(dialog.FileName);
            SetStatus($"已导出到: {dialog.FileName}", success: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ExportCsv()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = "fnm_export.csv",
            Title = "导出备注到 CSV"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _importExportService.ExportToCsv(dialog.FileName);
            SetStatus($"已导出到: {dialog.FileName}", success: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Import()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "支持的文件 (*.json;*.csv)|*.json;*.csv|JSON 文件|*.json|CSV 文件|*.csv",
            Title = "导入备注"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var result = _importExportService.Import(dialog.FileName, ImportConflictResolution.Skip);
            LoadNotes();
            RefreshStats();
            SetStatus($"导入完成: {result.Imported} 成功, {result.Skipped} 跳过, {result.Failed} 失败", success: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CreateBackup()
    {
        try
        {
            if (_backupService is FileNoteManager.Core.Services.Implementation.BackupService bs)
            {
                var path = bs.CreateAutoBackup();
                RefreshStats();
                SetStatus($"备份已创建: {System.IO.Path.GetFileName(path)}", success: true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RegisterShell()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                MessageBox.Show("无法获取程序路径。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ShellRegistrar.Register(exePath);
            IsShellRegistered = true;
            SetStatus("右键菜单注册成功！", success: true);
            MessageBox.Show("右键菜单已注册。在文件/文件夹上右键可看到「编辑文件备注」选项。",
                "注册成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"注册失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void UnregisterShell()
    {
        try
        {
            ShellRegistrar.Unregister();
            IsShellRegistered = false;
            SetStatus("右键菜单已注销");
            MessageBox.Show("右键菜单已注销。", "注销成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"注销失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ──────────────────────────────────────────────────
    // Private Helpers
    // ──────────────────────────────────────────────────

    private void LoadNotes()
    {
        try
        {
            // Use repository directly — most reliable path, no dynamic mapping
            var entities = _noteRepository.GetAll()   // already .ToList()'d inside
                .OrderByDescending(e => e.UpdatedAt)
                .ToList();

            _allNotes = entities.Select(ToListItemFromEntity).ToList();
            ApplyFilter(SearchText);
            SetStatus($"共 {_allNotes.Count} 条备注");

            // (Re-)register all note paths with the file watcher
            foreach (var note in _allNotes)
                _fileWatcher.RegisterPath(note.Path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {ex.GetType().Name}: {ex.Message}";
            _allNotes = new List<NoteListItemViewModel>();
            Notes.Clear();
        }
    }

    private void OnFileSystemChanged(object? sender, FileChangedEventArgs e)
    {
        // FileSystemWatcher events arrive on a background thread — marshal to UI
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            LoadNotes();
            try { RefreshStats(); } catch { }

            var name = System.IO.Path.GetFileName((e.NewPath ?? e.OldPath).TrimEnd('\\', '/'));
            SetStatus(e.ChangeType switch
            {
                ChangeType.Renamed => $"已更新路径: {System.IO.Path.GetFileName(e.OldPath.TrimEnd('\\', '/'))} → {System.IO.Path.GetFileName((e.NewPath ?? e.OldPath).TrimEnd('\\', '/'))}",
                ChangeType.Deleted => $"资源已删除, 备注已归档: {name}",
                _ => $"文件变更: {name}"
            });
        });
    }

    private void ApplyFilter(string keyword)
    {
        IEnumerable<NoteListItemViewModel> filtered;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            filtered = _allNotes;
        }
        else
        {
            var kw = keyword.Trim().ToLowerInvariant();
            filtered = _allNotes.Where(n =>
                n.DisplayName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                n.ContentPreview.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                n.Tags.Any(t => t.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        Notes.Clear();
        foreach (var item in filtered)
            Notes.Add(item);
    }

    private void RefreshNoteInList(string path)
    {
        var note = _noteService.GetNote(path);
        if (note == null) return;

        var newItem = ToListItem(note);
        var idx = _allNotes.FindIndex(n => n.Path == path);
        if (idx >= 0) _allNotes[idx] = newItem;
        else _allNotes.Insert(0, newItem);

        var listIdx = Notes.IndexOf(Notes.FirstOrDefault(n => n.Path == path)!);
        if (listIdx >= 0) Notes[listIdx] = newItem;
        else Notes.Insert(0, newItem);
    }

    private void RefreshStats()
    {
        try
        {
            var stats = _statsService.GetSummary();
            TotalNotes = stats.TotalNotes;
            var sizeKb = stats.DatabaseSize / 1024.0;
            DatabaseSizeDisplay = sizeKb < 1024
                ? $"{sizeKb:F1} KB"
                : $"{sizeKb / 1024:F1} MB";

            if (stats.LastBackupTime.HasValue)
            {
                var diff = DateTime.UtcNow - stats.LastBackupTime.Value;
                LastBackupDisplay = diff.TotalHours < 1
                    ? "刚刚"
                    : diff.TotalDays < 1
                        ? $"{(int)diff.TotalHours} 小时前"
                        : $"{(int)diff.TotalDays} 天前";
            }
            else
            {
                LastBackupDisplay = "从未";
            }

            IsShellRegistered = ShellRegistrar.IsRegistered();
        }
        catch
        {
            // Stats are non-critical
        }
    }

    private void SetStatus(string message, bool success = false)
    {
        StatusMessage = message;
    }

    private static NoteListItemViewModel ToListItemFromEntity(NoteEntity e)
    {
        var preview = e.Content?.Length > 80
            ? e.Content.Substring(0, 80) + "..."
            : e.Content ?? string.Empty;

        return new NoteListItemViewModel
        {
            Path = e.Path,
            DisplayName = System.IO.Path.GetFileName(e.Path.TrimEnd('\\', '/')) ?? e.Path,
            ContentPreview = preview,
            Tags = new List<string>(),
            TagsDisplay = string.Empty,
            UpdatedAt = e.UpdatedAt,
            IsFolder = e.IsFolder
        };
    }

    private static NoteListItemViewModel ToListItem(SearchResult r)
    {
        var preview = r.Content?.Length > 80
            ? r.Content.Substring(0, 80) + "..."
            : r.Content ?? string.Empty;

        return new NoteListItemViewModel
        {
            Path = r.Path,
            DisplayName = System.IO.Path.GetFileName(r.Path.TrimEnd('\\', '/')) ?? r.Path,
            ContentPreview = preview,
            Tags = r.Tags ?? new List<string>(),
            TagsDisplay = string.Join(" · ", r.Tags ?? Enumerable.Empty<string>()),
            UpdatedAt = r.UpdatedAt,
            IsFolder = r.IsFolder
        };
    }

    private static NoteListItemViewModel ToListItem(Note note)
    {
        var preview = note.Content?.Length > 80
            ? note.Content.Substring(0, 80) + "..."
            : note.Content ?? string.Empty;

        return new NoteListItemViewModel
        {
            Path = note.Path,
            DisplayName = System.IO.Path.GetFileName(note.Path.TrimEnd('\\', '/')) ?? note.Path,
            ContentPreview = preview,
            Tags = note.Tags ?? new List<string>(),
            TagsDisplay = string.Join(" · ", note.Tags ?? Enumerable.Empty<string>()),
            UpdatedAt = note.UpdatedAt,
            IsFolder = note.IsFolder
        };
    }
}
