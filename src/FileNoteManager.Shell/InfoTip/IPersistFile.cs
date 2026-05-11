using System.Runtime.InteropServices;

namespace FileNoteManager.Shell.InfoTip;

/// <summary>
/// Subset of IPersistFile that the shell uses to pass the file path to our handler.
/// IID = {0000010b-0000-0000-C000-000000000046}
///
/// All methods must have [PreserveSig] + int HRESULT return so the COM vtable
/// layout exactly matches the native IPersistFile definition.
/// </summary>
[ComVisible(true)]
[Guid("0000010b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPersistFile
{
    // IPersist::GetClassID
    [PreserveSig]
    int GetClassID(out Guid pClassID);

    // IPersistFile::IsDirty  — S_FALSE (1) = not dirty
    [PreserveSig]
    int IsDirty();

    [PreserveSig]
    int Load(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
        uint dwMode);

    [PreserveSig]
    int Save(
        [MarshalAs(UnmanagedType.LPWStr)] string? pszFileName,
        [MarshalAs(UnmanagedType.Bool)] bool fRemember);

    [PreserveSig]
    int SaveCompleted(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

    [PreserveSig]
    int GetCurFile(
        [MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
