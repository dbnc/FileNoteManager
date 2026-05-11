namespace FileNoteManager.Shell;

/// <summary>
/// Constants used for Windows registry shell integration
/// </summary>
public static class ShellConstants
{
    /// <summary>
    /// The registry key name for the context menu entry
    /// </summary>
    public const string MenuKeyName = "FileNoteManager";

    /// <summary>
    /// Display text shown in the right-click context menu
    /// </summary>
    public const string MenuDisplayText = "编辑文件备注(&N)";

    /// <summary>
    /// Command-line argument passed to the UI executable with the selected path
    /// </summary>
    public const string PathArgument = "--path";

    /// <summary>
    /// HKCU base path for per-user class registrations (no admin required)
    /// </summary>
    public const string HkcuClassesRoot = @"Software\Classes";

    /// <summary>
    /// Registry subpath for files (*) context menu
    /// </summary>
    public const string FileShellSubKey = @"*\shell\" + MenuKeyName;

    /// <summary>
    /// Registry subpath for directory context menu
    /// </summary>
    public const string DirectoryShellSubKey = @"Directory\shell\" + MenuKeyName;

    /// <summary>
    /// Registry subpath for directory background context menu
    /// </summary>
    public const string DirectoryBgShellSubKey = @"Directory\Background\shell\" + MenuKeyName;

    // ── InfoTip (hover tooltip) ──────────────────────────────────────────

    /// <summary>
    /// Windows shell extension category GUID for InfoTip handlers
    /// </summary>
    public const string InfoTipHandlerGuid = "{00021500-0000-0000-C000-000000000046}";

    /// <summary>
    /// CLSID of our InfoTip COM class (matches FileNoteInfoTipHandler.InfoTipClsid)
    /// </summary>
    public const string InfoTipClsid = "{8FE2DF3A-44AA-4AE4-B823-6D34C0E59A1A}";

    /// <summary>
    /// Registry path for per-file (*) InfoTip handler
    /// </summary>
    public const string FileInfoTipSubKey = @"*\shellex\" + InfoTipHandlerGuid;

    /// <summary>
    /// Registry path for directory InfoTip handler
    /// </summary>
    public const string DirectoryInfoTipSubKey = @"Directory\shellex\" + InfoTipHandlerGuid;

    /// <summary>
    /// CLSID registry subkey under HKCU\Software\Classes\CLSID
    /// </summary>
    public const string ClsidSubKey = @"Software\Classes\CLSID\" + InfoTipClsid;
}
