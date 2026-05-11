using CommunityToolkit.Mvvm.ComponentModel;

namespace FileNoteManager.UI.ViewModels;

/// <summary>
/// Represents a single note item shown in the left-panel list.
/// </summary>
public partial class NoteListItemViewModel : ObservableObject
{
    /// <summary>Full file/folder path</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>File or folder name only (for display)</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Truncated content preview (max 80 chars)</summary>
    public string ContentPreview { get; init; } = string.Empty;

    /// <summary>Tags joined for display</summary>
    public string TagsDisplay { get; init; } = string.Empty;

    /// <summary>Raw tag list for filtering</summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>Last update timestamp</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Whether this item is a folder</summary>
    public bool IsFolder { get; init; }

    /// <summary>Human-friendly relative time (e.g. "2 小时前")</summary>
    public string UpdatedDisplay => FormatRelativeTime(UpdatedAt);

    /// <summary>Icon character for display (folder vs file)</summary>
    public string Icon => IsFolder ? "📁" : "📄";

    private static string FormatRelativeTime(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalMinutes < 1) return "刚刚";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} 分钟前";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} 小时前";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} 天前";
        return utcTime.ToLocalTime().ToString("yyyy-MM-dd");
    }
}
