using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileNoteManager.Core.Interfaces;

namespace FileNoteManager.UI.ViewModels;

/// <summary>
/// ViewModel for the quick-edit popup window launched from Shell context menu.
/// </summary>
public partial class QuickNoteViewModel : ObservableObject
{
    private readonly INoteService _noteService;
    private readonly IFileWatcherService _fileWatcher;

    public event Action? RequestClose;

    public QuickNoteViewModel(INoteService noteService, IFileWatcherService fileWatcher)
    {
        _noteService  = noteService;
        _fileWatcher  = fileWatcher;
        Tags = new ObservableCollection<string>();
    }

    // ──────────────────────────────────────────────────
    // Observable Properties
    // ──────────────────────────────────────────────────

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tags;

    [ObservableProperty]
    private string _tagInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasExistingNote;

    // ──────────────────────────────────────────────────
    // Public Methods
    // ──────────────────────────────────────────────────

    /// <summary>
    /// Load the note (if any) for the given path.
    /// </summary>
    public void LoadPath(string path)
    {
        FilePath = path;
        FileName = System.IO.Path.GetFileName(path.TrimEnd('\\', '/')) ?? path;

        var note = _noteService.GetNote(path);
        bool migrated = false;
        if (note == null)
        {
            // File may have been moved from another directory — try to migrate
            note = _noteService.TryMigrateNote(path);
            migrated = note != null;
        }

        if (note != null)
        {
            Content = note.Content;
            Tags.Clear();
            foreach (var tag in note.Tags) Tags.Add(tag);
            HasExistingNote = true;
            if (migrated)
                StatusMessage = "已从原路径自动迁移备注";
        }
        else
        {
            Content = string.Empty;
            Tags.Clear();
            HasExistingNote = false;
        }
    }

    // ──────────────────────────────────────────────────
    // Commands
    // ──────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(FilePath)) return;

        if (string.IsNullOrWhiteSpace(Content))
        {
            var confirm = MessageBox.Show(
                "内容为空，是否删除此备注？",
                "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                _noteService.DeleteNote(FilePath);
            }
            RequestClose?.Invoke();
            return;
        }

        try
        {
            _noteService.UpdateNote(FilePath, Content, Tags);
            // Ensure the watcher tracks this path so future renames/deletes
            // are reflected in the database even without the main window open.
            _fileWatcher.RegisterPath(FilePath);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (string.IsNullOrWhiteSpace(FilePath)) return;

        var result = MessageBox.Show(
            "确认删除此备注？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _noteService.DeleteNote(FilePath);
            RequestClose?.Invoke();
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
        if (string.IsNullOrWhiteSpace(tag) || Tags.Contains(tag)) return;
        Tags.Add(tag);
        TagInput = string.Empty;
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        Tags.Remove(tag);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
