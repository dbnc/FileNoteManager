using System.Runtime.InteropServices;
using FileNoteManager.Core.Data;
using FileNoteManager.Core.Repositories.Implementation;

namespace FileNoteManager.Shell.InfoTip;

/// <summary>
/// Windows Explorer InfoTip (hover tooltip) handler.
///
/// Registered under HKCU\Software\Classes\*\shellex\{00021500-0000-0000-C000-000000000046}
/// so Explorer calls it for every file/folder hover event.
///
/// Explorer calls:
///   1. IPersistFile.Load(path)  — tells us which file
///   2. IQueryInfo.GetInfoTip()  — we return the note content
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid(InfoTipClsid)]
public class FileNoteInfoTipHandler : IQueryInfo, IPersistFile
{
    /// <summary>CLSID of this COM class — must match ShellConstants.InfoTipClsid</summary>
    public const string InfoTipClsid = "8FE2DF3A-44AA-4AE4-B823-6D34C0E59A1A";

    private string? _filePath;

    // ── IQueryInfo ──────────────────────────────────────────────

    public int GetInfoTip(uint dwFlags, out string? ppwszTip)
    {
        ppwszTip = null;
        if (_filePath == null) return 0;

        try
        {
            var note = QueryNote(_filePath);
            if (!string.IsNullOrWhiteSpace(note))
            {
                // Truncate long notes in the tooltip (max 300 chars)
                ppwszTip = note.Length > 300
                    ? note.Substring(0, 297) + "..."
                    : note;
            }
        }
        catch
        {
            // Never crash Explorer — swallow all exceptions
        }

        return 0; // S_OK
    }

    public int GetInfoFlags(out uint pdwFlags)
    {
        pdwFlags = 0;
        return 0; // S_OK
    }

    // ── IPersistFile ────────────────────────────────────────────

    public int GetClassID(out Guid pClassID)
    {
        pClassID = new Guid(InfoTipClsid);
        return 0; // S_OK
    }

    public int IsDirty() => 1; // S_FALSE — never dirty

    public int Load(string pszFileName, uint dwMode)
    {
        _filePath = pszFileName;
        return 0; // S_OK
    }

    public int Save(string? pszFileName, bool fRemember) => 0;
    public int SaveCompleted(string pszFileName) => 0;
    public int GetCurFile(out string ppszFileName)
    {
        ppszFileName = _filePath ?? string.Empty;
        return 0; // S_OK
    }

    // ── Database query ──────────────────────────────────────────

    private static string? QueryNote(string path)
    {
        // ── Primary: SQLite database ────────────────────────────
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileNoteManager", "fnm.db");

        if (System.IO.File.Exists(dbPath))
        {
            try
            {
                using var dbManager = new DatabaseManager(dbPath);
                var repo = new NoteRepository(dbManager);
                var entity = repo.GetById(path);
                if (entity != null && !entity.IsDeleted && !string.IsNullOrWhiteSpace(entity.Content))
                    return entity.Content;
            }
            catch { }
        }

        // ── Fallback: NTFS ADS :fnm (travels with file on move/copy) ──
        // Only for files (not directories — ADS on directories is unreliable)
        if (!System.IO.Directory.Exists(path))
        {
            try
            {
                var ads = System.IO.File.ReadAllText(path + ":fnm");
                if (!string.IsNullOrWhiteSpace(ads)) return ads;
            }
            catch { }
        }

        return null;
    }
}
