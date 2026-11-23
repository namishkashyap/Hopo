namespace Lantoss.Core;

/// <summary>The shortcut that opens the device ring for the files selected in File Explorer or on the desktop.</summary>
public static class SendShortcut
{
    public const string Default = "Ctrl+Shift+S", Plain = "Shift+S", Off = "Off";
    public static readonly string[] Choices = [Default, Plain, Off];
    public static bool Valid(string? value) => value is Default or Plain or Off;
    /// <summary>
    /// True when a key press opens the ring: S (virtual key 0x53) with exactly the chosen modifiers, while the keyboard focus is the file list
    /// of a File Explorer window or of the desktop, with no caret shown. A caret means typing: renaming, searching or the address bar.
    /// </summary>
    public static bool Matches(string? setting, int key, bool ctrl, bool shift, bool alt, bool windows, string window, string parent, string focus, bool caret)
    {
        if (key != 0x53 || !shift || alt || windows || caret || setting switch
        {
            Default => !ctrl,
            Plain => ctrl,
            _ => true
        })
            return false;
        // Explorer's file list is a DirectUIHWND, the desktop's a SysListView32, each directly inside SHELLDLL_DefView.
        return parent == "SHELLDLL_DefView" && (window == "CabinetWClass" && focus == "DirectUIHWND" || window is "Progman" or "WorkerW" && focus == "SysListView32");
    }
}
