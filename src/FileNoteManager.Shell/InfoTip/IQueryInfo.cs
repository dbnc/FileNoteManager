using System.Runtime.InteropServices;

namespace FileNoteManager.Shell.InfoTip;

/// <summary>
/// COM interface for Windows Explorer InfoTip (tooltip on file hover).
/// IID = {00021500-0000-0000-C000-000000000046}
/// </summary>
[ComVisible(true)]
[Guid("00021500-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IQueryInfo
{
    [PreserveSig]
    int GetInfoTip(uint dwFlags,
        [MarshalAs(UnmanagedType.LPWStr)] out string? ppwszTip);

    [PreserveSig]
    int GetInfoFlags(out uint pdwFlags);
}
