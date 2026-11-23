using System.Text.Json.Nodes;
using Lantoss.Core;

internal static partial class Program
{
    // The Explorer send ring (docs/changes/2026-11-24-hopo-send-ring.md): the key rule and the shortcut preference.
    private static void SendRingCases()
    {
        const string list = "DirectUIHWND", view = "SHELLDLL_DefView", explorer = "CabinetWClass";
        Check(new Preferences().SendShortcut == SendShortcut.Default && SendShortcut.Default == "Ctrl+Shift+S", "The send ring opens with Ctrl+Shift+S unless changed");
        Check(SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, false, explorer, view, list, false), "Ctrl+Shift+S in an Explorer file list opens the ring");
        Check(SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, false, "Progman", view, "SysListView32", false) && SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, false, "WorkerW", view, "SysListView32", false), "Ctrl+Shift+S on the desktop opens the ring");
        Check(!SendShortcut.Matches(SendShortcut.Default, 0x53, false, true, false, false, explorer, view, list, false), "With Ctrl+Shift+S chosen, Shift+S still types or jumps to S in Explorer");
        Check(SendShortcut.Matches(SendShortcut.Plain, 0x53, false, true, false, false, explorer, view, list, false) && !SendShortcut.Matches(SendShortcut.Plain, 0x53, true, true, false, false, explorer, view, list, false), "Shift+S can be chosen instead, and then Ctrl+Shift+S is left alone");
        Check(!SendShortcut.Matches(SendShortcut.Off, 0x53, true, true, false, false, explorer, view, list, false) && !SendShortcut.Matches(SendShortcut.Off, 0x53, false, true, false, false, explorer, view, list, false), "Off never opens the ring");
        Check(!SendShortcut.Matches(SendShortcut.Plain, 0x53, false, true, false, false, explorer, list, "Edit", true) && !SendShortcut.Matches(SendShortcut.Plain, 0x53, false, true, false, false, explorer, view, list, true), "Typing a capital S while renaming, or anywhere a caret shows, is never captured");
        Check(!SendShortcut.Matches(SendShortcut.Plain, 0x53, false, true, false, false, explorer, "UniversalSearchBand", list, false), "Explorer's search and address boxes keep their keys");
        Check(!SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, false, "Notepad", view, list, false) && !SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, false, "Chrome_WidgetWin_1", "Chrome_WidgetWin_1", "Chrome_RenderWidgetHostHWND", false), "Other apps keep Ctrl+Shift+S (Save As and the like)");
        Check(!SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, true, false, explorer, view, list, false) && !SendShortcut.Matches(SendShortcut.Default, 0x53, true, true, false, true, explorer, view, list, false) && !SendShortcut.Matches(SendShortcut.Default, 0x41, true, true, false, false, explorer, view, list, false), "Extra Alt or Windows keys, or another letter, do not open the ring");
        MetadataStore store = Store("Send shortcut");
        bool refused;
        try
        {
            store.SetPreferences(store.Preferences with
            {
                SendShortcut = "Ctrl+Q"
            });
            refused = false;
        }
        catch (InvalidDataException) { refused = true; }
        store.SetPreferences(store.Preferences with
        {
            SendShortcut = SendShortcut.Plain
        });
        Check(refused && new MetadataStore(store.Root).Preferences.SendShortcut == SendShortcut.Plain, "The send shortcut saves, and an unknown shortcut is refused");
        string file = Path.Combine(store.Root, "metadata.json");
        JsonNode saved = JsonNode.Parse(File.ReadAllText(file))!;
        saved["Preferences"]!["SendShortcut"] = "Ctrl+Q";
        saved["Preferences"]!["DeviceName"] = "Kept PC";
        File.WriteAllText(file, saved.ToJsonString());
        Preferences reloaded = new MetadataStore(store.Root).Preferences;
        Check(reloaded.SendShortcut == SendShortcut.Default && reloaded.DeviceName == "Kept PC", "A damaged saved shortcut falls back to Ctrl+Shift+S and keeps the other settings");
    }
}
