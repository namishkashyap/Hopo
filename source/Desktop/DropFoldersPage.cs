using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lantoss.Desktop;

/// <summary>Drop folders: send straight into a folder another device shares, and (on computers) the folders this one shares. A phone only sends.</summary>
internal sealed class DropFoldersPage : IPage
{
    private readonly Shell app;
    private readonly StackPanel dropList = new(), devices = new();
    public Control View
    {
        get;
    }
    public DropFoldersPage(Shell app)
    {
        this.app = app;
        StackPanel send = C.Stack(C.Heading("Send into a folder on a PC"), C.Space(C.Text("Tap Choose files next to a PC, pick the files, then open the folder to put them in and tap Send here. You can also make a new folder there.", 12, "Muted"), 14), devices);
        Border sendCard = C.Card(send);
        if (OperatingSystem.IsAndroid())
            View = C.Stack(C.Header("Share", "Drop folders", "Send files straight into a folder on your PC."), C.Space(sendCard, 16, 20),
                C.Note("Nothing to see?", "On the PC, open hopo, choose Drop folders in the menu and add a folder. Only those folders show here, and never the files in them."));
        else
        {
            Button add = C.AsyncButton("Add drop folder", AddFoldersAsync, true, "plus");
            StackPanel share = C.Stack(C.Heading("Folders on this " + Shell.Platform + " that others can send into"), C.Space(C.Text("Paired phones and PCs see these folders by name, open the folders inside them, and can make a new folder there. They never see your files. Devices set to Always accept send without asking; others need your approval.", 12, "Muted"), 14), C.Space(dropList, 14), add);
            View = C.Stack(C.Header("Share", "Drop folders", "Send files straight into a folder you choose, from a phone or another computer."), C.Space(C.Columns(C.Card(share), sendCard, 420), 20, 20));
        }
    }
    public void Refresh()
    {
        dropList.Children.Clear();
        string[] drops = app.Store.Preferences.DropFolders;
        if (drops.Length == 0)
            dropList.Children.Add(C.Text("No folders shared yet. Add one to let your phone or other computers send into it.", 13, "Muted"));
        foreach (string drop in drops)
            dropList.Children.Add(Row("folder", Path.GetFileName(drop), drop, "Muted", C.Small("Remove", () => Save(app.Store.Preferences.DropFolders.Where(folder => folder != drop), Path.GetFileName(drop) + " is no longer shared."))));
        devices.Children.Clear();
        Peer[] paired = app.Engine.Peers.Where(peer => peer.Paired && !peer.Blocked).ToArray();
        if (paired.Length == 0)
            devices.Children.Add(C.Stack(C.Space(C.Text("No paired PCs yet. Pair one on the Devices page first.", 13, "Muted"), 10), C.Button("Open Devices", () => app.Navigate("Devices"), icon: "devices")));
        foreach (Peer peer in paired)
            devices.Children.Add(Row("devices", peer.Name, peer.Online ? "Online" : "Offline: open hopo on it first", peer.Online ? "Success" : "Muted", C.Small("Choose files", () => ChooseAndSendAsync(peer))));
    }
    private static DockPanel Row(string icon, string title, string detail, string detailColor, Button action)
    {
        DockPanel.SetDock(action, Dock.Right);
        Control image = C.Icon(icon, 20, "Primary");
        image.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(image, Dock.Left);
        TextBlock name = C.Text(title, 14, bold: true), small = C.Text(detail, 11, detailColor);
        name.TextTrimming = small.TextTrimming = TextTrimming.CharacterEllipsis;
        small.TextWrapping = TextWrapping.NoWrap;
        StackPanel text = C.Stack(name, small);
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Margin = new(10, 0, 10, 0);
        ToolTip.SetTip(text, detail);
        return new DockPanel { Margin = new(0, 0, 0, 10), LastChildFill = true, Children = { action, image, text } };
    }
    private async Task AddFoldersAsync()
    {
        string[] picked = await app.PickFoldersAsync("Choose a folder phones and PCs can send into", true);
        if (picked.Length > 0)
            Save(app.Store.Preferences.DropFolders.Concat(picked.Select(MetadataStore.CheckDropFolder)), "Paired phones and PCs can now send into " + string.Join(", ", picked.Select(Path.GetFileName)) + ".");
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
    private async Task ChooseAndSendAsync(Peer peer)
    {
        string[] paths = await app.PickFilesAsync("Choose files to send to " + peer.Name);
        if (paths.Length > 0)
            await app.SendToFolderAsync(peer, paths);
    }
}
