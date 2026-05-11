using FileNoteManager.Core.Interfaces;

namespace FileNoteManager.Core.Services.Implementation;

/// <summary>
/// Watches parent directories of all registered note paths.
/// One FileSystemWatcher per unique parent directory to avoid duplicate events.
/// Handles same-directory rename → UpdatePath, delete → MarkAsDeleted.
/// Cross-directory moves (cut+paste) appear as Deleted and are marked accordingly.
/// </summary>
public class FileWatcherService : IFileWatcherService, IDisposable
{
    private readonly INoteService _noteService;

    // Key = parent directory being watched
    private readonly Dictionary<string, FileSystemWatcher> _watchers =
        new(StringComparer.OrdinalIgnoreCase);

    // Key = parent directory, Value = set of full note paths within that directory
    private readonly Dictionary<string, HashSet<string>> _dirToNotes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();
    private bool _isRunning;
    private bool _disposed;

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public FileWatcherService(INoteService noteService)
    {
        _noteService = noteService;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
            foreach (var w in _watchers.Values)
                w.EnableRaisingEvents = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            foreach (var w in _watchers.Values)
                w.EnableRaisingEvents = false;
        }
    }

    public void RegisterPath(string path)
    {
        var dir = GetParentDirectory(path);
        if (dir == null) return;

        lock (_lock)
        {
            if (!_dirToNotes.TryGetValue(dir, out var notes))
            {
                notes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _dirToNotes[dir] = notes;
                CreateWatcherFor(dir);
            }
            notes.Add(path);
        }
    }

    public void UnregisterPath(string path)
    {
        var dir = GetParentDirectory(path);
        if (dir == null) return;

        lock (_lock)
        {
            if (!_dirToNotes.TryGetValue(dir, out var notes)) return;
            notes.Remove(path);

            if (notes.Count == 0)
            {
                _dirToNotes.Remove(dir);
                RemoveWatcherFor(dir);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Event handlers
    // ──────────────────────────────────────────────────────────────────────

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            string? oldTracked = FindTracked(e.OldFullPath);
            if (oldTracked == null) return;
            if (!_noteService.HasNote(oldTracked)) return;

            _noteService.UpdatePath(oldTracked, e.FullPath);

            lock (_lock)
            {
                var dir = GetParentDirectory(oldTracked);
                if (dir != null && _dirToNotes.TryGetValue(dir, out var notes))
                {
                    notes.Remove(oldTracked);
                    notes.Add(e.FullPath);   // track new path in same directory
                }
            }

            FileChanged?.Invoke(this, new FileChangedEventArgs
            {
                OldPath = oldTracked,
                NewPath = e.FullPath,
                ChangeType = ChangeType.Renamed
            });
        }
        catch { }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            string? tracked = FindTracked(e.FullPath);
            if (tracked == null) return;
            if (!_noteService.HasNote(tracked)) return;

            _noteService.MarkAsDeleted(tracked);

            lock (_lock)
            {
                var dir = GetParentDirectory(tracked);
                if (dir != null && _dirToNotes.TryGetValue(dir, out var notes))
                    notes.Remove(tracked);
            }

            FileChanged?.Invoke(this, new FileChangedEventArgs
            {
                OldPath = tracked,
                ChangeType = ChangeType.Deleted
            });
        }
        catch { }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private string? FindTracked(string fullPath)
    {
        lock (_lock)
        {
            var dir = GetParentDirectory(fullPath);
            if (dir != null && _dirToNotes.TryGetValue(dir, out var notes))
                return notes.FirstOrDefault(n =>
                    string.Equals(n, fullPath, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }

    private void CreateWatcherFor(string dir)
    {
        if (!Directory.Exists(dir)) return;
        if (_watchers.ContainsKey(dir)) return;

        var w = new FileSystemWatcher(dir)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = _isRunning
        };
        w.Renamed += OnRenamed;
        w.Deleted += OnDeleted;
        _watchers[dir] = w;
    }

    private void RemoveWatcherFor(string dir)
    {
        if (!_watchers.TryGetValue(dir, out var w)) return;
        w.EnableRaisingEvents = false;
        w.Renamed -= OnRenamed;
        w.Deleted -= OnDeleted;
        w.Dispose();
        _watchers.Remove(dir);
    }

    /// <summary>
    /// Returns the directory to watch for a given note path.
    /// For a folder note (e.g. C:\Projects\MyFolder), watch C:\Projects so
    /// renaming the folder itself is detected.
    /// For a file note (e.g. C:\Docs\file.txt), watch C:\Docs.
    /// </summary>
    private static string? GetParentDirectory(string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        return Path.GetDirectoryName(trimmed);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        lock (_lock)
        {
            foreach (var w in _watchers.Values)
            {
                w.Renamed -= OnRenamed;
                w.Deleted -= OnDeleted;
                w.Dispose();
            }
            _watchers.Clear();
            _dirToNotes.Clear();
        }
    }
}
