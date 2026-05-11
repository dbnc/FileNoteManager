using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FileNoteManager.Shell;

/// <summary>
/// Manages Windows registry entries for right-click context menu integration.
/// Uses HKCU (no administrator privileges required).
/// </summary>
public static class ShellRegistrar
{
    /// <summary>
    /// Register the context menu AND hover InfoTip handler for files and folders.
    /// Writes to HKCU\Software\Classes — no admin rights needed.
    /// </summary>
    /// <param name="exePath">Full path to FileNoteManager.UI.exe</param>
    public static void Register(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            throw new ArgumentException("Executable path cannot be empty", nameof(exePath));

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Executable not found: {exePath}");

        try
        {
            // ── Right-click context menu ──────────────────────────
            RegisterMenuEntry(ShellConstants.FileShellSubKey, exePath, usePathVar: true);
            RegisterMenuEntry(ShellConstants.DirectoryShellSubKey, exePath, usePathVar: true);
            RegisterMenuEntry(ShellConstants.DirectoryBgShellSubKey, exePath, usePathVar: false);

            // ── Hover InfoTip (comhost.dll) ───────────────────────
            var comhostPath = Path.Combine(
                Path.GetDirectoryName(exePath)!,
                "FileNoteManager.Shell.comhost.dll");

            if (File.Exists(comhostPath))
            {
                RegisterInfoTip(comhostPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to register shell extensions.", ex);
        }

        NotifyShell();
    }

    /// <summary>
    /// Remove all FileNoteManager shell extension entries from the registry.
    /// </summary>
    public static void Unregister()
    {
        try
        {
            DeleteMenuEntry(ShellConstants.FileShellSubKey);
            DeleteMenuEntry(ShellConstants.DirectoryShellSubKey);
            DeleteMenuEntry(ShellConstants.DirectoryBgShellSubKey);
            UnregisterInfoTip();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to unregister shell extensions.", ex);
        }

        NotifyShell();
    }

    /// <summary>
    /// Check whether the context menu is currently registered.
    /// </summary>
    public static bool IsRegistered()
    {
        using var hkcu = Registry.CurrentUser;
        var keyPath = $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.FileShellSubKey}";
        using var key = hkcu.OpenSubKey(keyPath);
        return key != null;
    }

    /// <summary>
    /// Get the executable path currently registered, or null if not registered.
    /// </summary>
    public static string? GetRegisteredExePath()
    {
        using var hkcu = Registry.CurrentUser;
        var commandKeyPath = $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.FileShellSubKey}\command";
        using var commandKey = hkcu.OpenSubKey(commandKeyPath);
        if (commandKey == null) return null;

        var value = commandKey.GetValue(null) as string;
        if (value == null) return null;

        // Parse: "C:\path\to\fnm.exe" --path "%1"  →  extract the exe path
        value = value.Trim();
        if (value.StartsWith("\""))
        {
            var endQuote = value.IndexOf('"', 1);
            return endQuote > 0 ? value.Substring(1, endQuote - 1) : null;
        }

        var spaceIndex = value.IndexOf(' ');
        return spaceIndex > 0 ? value.Substring(0, spaceIndex) : value;
    }

    private static void RegisterMenuEntry(string subKey, string exePath, bool usePathVar)
    {
        var fullSubKey = $@"{ShellConstants.HkcuClassesRoot}\{subKey}";
        var commandSubKey = $@"{fullSubKey}\command";

        // Create or open the menu key
        using (var menuKey = Registry.CurrentUser.CreateSubKey(fullSubKey, writable: true))
        {
            menuKey.SetValue(null, ShellConstants.MenuDisplayText);
            menuKey.SetValue("Icon", $"\"{exePath}\",0");
        }

        // Create or update the command key
        using var cmdKey = Registry.CurrentUser.CreateSubKey(commandSubKey, writable: true);
        var commandValue = usePathVar
            ? $"\"{exePath}\" {ShellConstants.PathArgument} \"%1\""
            : $"\"{exePath}\"";
        cmdKey.SetValue(null, commandValue);
    }

    private static void DeleteMenuEntry(string subKey)
    {
        var fullSubKey = $@"{ShellConstants.HkcuClassesRoot}\{subKey}";
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(fullSubKey, throwOnMissingSubKey: false);
        }
        catch (Exception)
        {
            // If key doesn't exist, silently succeed
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    /// <summary>Notify Explorer to reload shell handler registrations immediately.</summary>
    private static void NotifyShell()
    {
        try
        {
            // SHCNE_ASSOCCHANGED | SHCNF_IDLIST | SHCNF_FLUSH
            SHChangeNotify(0x08000000, 0x00001000, IntPtr.Zero, IntPtr.Zero);
        }
        catch { }
    }

    private static void RegisterInfoTip(string comhostPath)
    {
        // 1. Register COM class under HKCU\Software\Classes\CLSID\{CLSID}
        using (var clsidKey = Registry.CurrentUser.CreateSubKey(ShellConstants.ClsidSubKey, writable: true))
        {
            clsidKey.SetValue(null, "FileNoteManager InfoTip Handler");
        }
        using (var inproc = Registry.CurrentUser.CreateSubKey(
            $@"{ShellConstants.ClsidSubKey}\InprocServer32", writable: true))
        {
            inproc.SetValue(null, comhostPath);
            inproc.SetValue("ThreadingModel", "Apartment");
            // Do NOT set RuntimeVersion — that is a .NET Framework 1.x/2.x/4.x hint.
            // .NET 8 comhost.dll reads its own runtimeconfig.json to find the runtime.
        }

        // 2. Add to the per-user Approved shell extensions list (required on Windows 11)
        using (var approved = Registry.CurrentUser.CreateSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", writable: true))
        {
            approved.SetValue(ShellConstants.InfoTipClsid, "FileNoteManager InfoTip Handler");
        }

        // 3. Register InfoTip handler for files (*)
        using (var fileKey = Registry.CurrentUser.CreateSubKey(
            $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.FileInfoTipSubKey}", writable: true))
        {
            fileKey.SetValue(null, ShellConstants.InfoTipClsid);
        }

        // 4. Register InfoTip handler for directories
        using (var dirKey = Registry.CurrentUser.CreateSubKey(
            $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.DirectoryInfoTipSubKey}", writable: true))
        {
            dirKey.SetValue(null, ShellConstants.InfoTipClsid);
        }
    }

    private static void UnregisterInfoTip()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(ShellConstants.ClsidSubKey, false); } catch { }
        try
        {
            using var approved = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", writable: true);
            approved?.DeleteValue(ShellConstants.InfoTipClsid, throwOnMissingValue: false);
        }
        catch { }
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.FileInfoTipSubKey}", false);
        }
        catch { }
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"{ShellConstants.HkcuClassesRoot}\{ShellConstants.DirectoryInfoTipSubKey}", false);
        }
        catch { }
    }
}
