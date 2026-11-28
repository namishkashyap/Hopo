using Microsoft.Win32;

namespace Lantoss.UI;

/// <summary>Drop folders: the folders on this PC that paired phones and PCs may send straight into, and sending into another device's drop folders.</summary>
internal sealed class DropFoldersPage : IPage
{
    private readonly AppShell app;
    private readonly StackPanel dropList = new(), devices = new();
    public FrameworkElement View
    {
        get;
    }
    public DropFoldersPage(AppShell app)
    {
        this.app = app;
        Button add = C.Button("Add drop folder", AddFolders, true, "plus");
        StackPanel share = C.Stack(C.Heading("Folders on this PC that others can send into"), C.Space(C.Text("Paired phones and PCs see these folders by name, open the folders inside them, and can make a new folder there. They never see your files. Devices set to Always accept send without asking; others need your approval.", 12, "Muted"), 14), C.Space(dropList, 14), add);
        StackPanel send = C.Stack(C.Heading("Send into a folder on another device"), C.Space(C.Text("Choose Files or Folder next to a device, then open the folder to put them in, or create a new one there. That device must have added drop folders on its own Drop folders page.", 12, "Muted"), 14), devices);
        View = C.Stack(C.Hero("hopo", "Drop folders", "Send files straight into a folder you choose, from a phone or another PC."), C.Space(C.Columns(C.Card(share), C.Card(send), 420), 20),
            C.Space(C.Note("How to send from your phone", "On the phone, open hopo, open the menu and choose Drop folders. Pick this PC, choose files, open a folder and tap Send here."), 0, 4));
    }
    public void Refresh()
    {
        dropList.Children.Clear();
        string[] drops = app.Store.Preferences.DropFolders;
        if (drops.Length == 0)
            dropList.Children.Add(C.Text("No folders shared yet. Add one to let your phone or other PCs send into it.", 13, "Muted"));
        foreach (string drop in drops)
        {
            Button remove = C.Small("Remove", () => Save(app.Store.Preferences.DropFolders.Where(folder => folder != drop), Path.GetFileName(drop) + " is no longer shared."));
            DockPanel.SetDock(remove, Dock.Right);
            TextBlock path = C.Text(drop, 11, "Muted");
            path.TextWrapping = TextWrapping.NoWrap;
            path.TextTrimming = TextTrimming.CharacterEllipsis;
            StackPanel text = C.Stack(C.Text(Path.GetFileName(drop), 14, bold: true), path);
            text.VerticalAlignment = VerticalAlignment.Center;
            text.ToolTip = drop;
            text.Margin = new(10, 0, 10, 0);
            FrameworkElement icon = C.Icon("folder", 20, "Primary");
            icon.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(icon, Dock.Left);
            dropList.Children.Add(new DockPanel { Margin = new(0, 0, 0, 10), LastChildFill = true, Children = { remove, icon, text } });
        }
        devices.Children.Clear();
        Peer[] paired = app.Engine.Peers.Where(peer => peer.Paired && !peer.Blocked).ToArray();
        if (paired.Length == 0)
            devices.Children.Add(C.Stack(C.Space(C.Text("No paired devices yet. Pair a PC on the Devices page first.", 13, "Muted"), 10), C.Button("Open Devices", () => app.Navigate("Devices"), icon: "devices")));
        foreach (Peer peer in paired)
        {
            Button choose = C.Small("Files…", () => ChooseAndSend(peer)), chooseFolder = C.Small("Folder…", () => { OpenFolderDialog picker = new() { Title = "Choose folders to send to " + peer.Name, Multiselect = true }; if (picker.ShowDialog(app.Window) == true) app.Dialogs.SendToFolder(peer, picker.FolderNames); });
            DockPanel.SetDock(chooseFolder, Dock.Right);
            DockPanel.SetDock(choose, Dock.Right);
            choose.Margin = new(0, 0, 6, 0);
            StackPanel text = C.Stack(C.Text(peer.Name, 14, bold: true), C.Text(peer.Online ? "Online" : "Offline: open hopo on it first", 11, peer.Online ? "Success" : "Muted"));
            text.VerticalAlignment = VerticalAlignment.Center;
            text.Margin = new(10, 0, 10, 0);
            FrameworkElement icon = C.Icon("devices", 20, "Primary");
            icon.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(icon, Dock.Left);
            devices.Children.Add(new DockPanel { Margin = new(0, 0, 0, 10), LastChildFill = true, Children = { chooseFolder, choose, icon, text } });
        }
    }
    private void AddFolders()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Choose a folder phones and PCs can send into",
            Multiselect = true
        };
        if (dialog.ShowDialog(app.Window) != true)
            return;
        Save(app.Store.Preferences.DropFolders.Concat(dialog.FolderNames.Select(MetadataStore.CheckDropFolder)), "Paired phones and PCs can now send into " + string.Join(", ", dialog.FolderNames.Select(Path.GetFileName)) + ".");
    }
    private void Save(IEnumerable<string> folders, string message)
    {
        app.Store.SetPreferences(app.Store.Preferences with
        {
            DropFolders = [.. folders]
        });
        Refresh();
        app.Tell(message);
    }
    private void ChooseAndSend(Peer peer)
    {
        OpenFileDialog picker = new()
        {
            Title = "Choose files to send to " + peer.Name,
            Multiselect = true,
            CheckFileExists = true
        };
        if (picker.ShowDialog(app.Window) == true)
            app.Dialogs.SendToFolder(peer, picker.FileNames);
    }
}
