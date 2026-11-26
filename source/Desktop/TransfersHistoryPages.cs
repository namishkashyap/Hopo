using System.Text;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Lantoss.Desktop;

internal sealed class TransfersPage : IPage
{
    private readonly Shell app;
    private readonly StackPanel rows = new();
    private readonly Dictionary<string, TransferRow> cards = [];
    private readonly Dictionary<string, Button> filters = [];
    private string filter = "All", layoutKey = "";
    private string shownPills = "";
    public Control View
    {
        get;
    }
    public TransfersPage(Shell app)
    {
        this.app = app;
        StackPanel tabs = C.Inline();
        foreach (string name in new[] { "All", "Active", "Queued", "Completed", "Failed" })
        {
            Button button = C.Small(name, () => { filter = name; layoutKey = ""; Refresh(); });
            button.CornerRadius = new(17);
            filters[name] = button;
            tabs.Children.Add(button);
        }
        View = C.Stack(C.Header("Manage", "Transfers", "Live progress, verified on arrival.", C.Button("Send files", () => app.Navigate("Send"), true, "send")), C.Space(tabs, 20), rows,
            C.Space(C.Text("Retry starts a new transfer and asks the recipient for approval again.", 11, "Muted"), 0, 20));
    }
    public void Refresh()
    {
        TransferRecord[] all = app.Engine.Transfers.Records.ToArray();
        // The filter pills are restyled only when the chosen filter or a count changes, not on every progress refresh.
        string pills = filter + "|" + all.Count(record => !record.Terminal && record.State != TransferState.Queued) + "|" + all.Count(record => record.State == TransferState.Queued) + "|" + Theme.Dark;
        foreach ((string name, Button button) in pills == shownPills ? Enumerable.Empty<KeyValuePair<string, Button>>() : filters)
        {
            // Filter pills: the chosen one carries the amber tint; Active and Queued show their counts.
            int count = name is "Active" or "Queued" ? all.Count(record => name == "Active" ? !record.Terminal && record.State != TransferState.Queued : record.State == TransferState.Queued) : 0;
            button.Content = count > 0 ? name + " · " + count : name;
            Theme.SetButton(button.Resources, name == filter ? "Accent/14" : "Glass", "Text", name == filter ? "Accent/45" : "GlassBorder", name == filter ? "Accent/20" : "Text/8");
        }
        TransferRecord[] records = app.Engine.Transfers.Records.Where(r => Matches(r) && (r.Title.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.PeerName.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.Files.Any(f => f.RelativePath.Contains(app.Search, StringComparison.OrdinalIgnoreCase)))).Take(100).ToArray();
        shownPills = pills;
        string key = string.Join('|', records.Select(r => r.Id + r.Direction));
        // The list is rearranged only when the set of jobs changes; cards still listed are kept (new transfers get a new card) and
        // progress updates reuse them.
        if (layoutKey != key || rows.Children.Count == 0)
        {
            layoutKey = key;
            rows.Children.Clear();
            Dictionary<string, TransferRow> kept = [];
            foreach (TransferRecord record in records)
                if (cards.TryGetValue(record.Id + record.Direction, out TransferRow? existing))
                    kept[record.Id + record.Direction] = existing;
            cards.Clear();
            foreach ((string id, TransferRow existing) in kept)
                cards[id] = existing;
            if (records.Length == 0)
                rows.Children.Add(C.Empty("Nothing in this queue.", "Send files from the Send screen, or approve an incoming transfer. Active and finished jobs appear here.", "transfer", C.AsyncButton("Choose files", app.PickFilesAsync, icon: "plus")));
            foreach (TransferRecord record in records)
            {
                if (!cards.TryGetValue(record.Id + record.Direction, out TransferRow? row))
                    cards[record.Id + record.Direction] = row = new(app, record);
                rows.Children.Add(C.Space(row.View, 14));
            }
        }
        foreach (TransferRecord record in records)
            if (cards.TryGetValue(record.Id + record.Direction, out TransferRow? row))
                row.Update(record);
    }
    private bool Matches(TransferRecord record) => filter switch
    {
        "Active" => !record.Terminal && record.State != TransferState.Queued,
        "Queued" => record.State == TransferState.Queued,
        "Completed" => record.State == TransferState.Completed,
        "Failed" => record.State == TransferState.Failed,
        _ => true
    };
}

internal sealed class TransferRow
{
    private readonly Shell app;
    private TransferRecord record;
    private readonly TextBlock title = C.Text("", 15, bold: true), status = C.Text("", 12, "Primary", true), detail = C.Text("", 11, "Muted"), current = C.Text("", 12), amount = C.Text("", 11, "Muted"), speed = C.Text("", 11, "Primary"), error = C.Text("", 12, "Danger");
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, Height = 6, MinHeight = 6, CornerRadius = new(3), Background = Theme.Brush("Text/8"), Margin = new(0, 12) };
    // A phone wraps the transfer buttons onto more lines.
    private readonly Panel buttons = OperatingSystem.IsAndroid() ? new WrapPanel() : C.Inline();
    private readonly StackPanel fileDetails = new() { IsVisible = false };
    private readonly Button details;
    private bool lastTerminal;
    public Border View
    {
        get;
    }
    public TransferRow(Shell app, TransferRecord record)
    {
        this.app = app;
        this.record = record;
        DockPanel heading = new();
        DockPanel.SetDock(status, Dock.Right);
        heading.Children.Add(status);
        heading.Children.Add(title);
        DockPanel counts = new();
        DockPanel.SetDock(speed, Dock.Right);
        counts.Children.Add(speed);
        counts.Children.Add(amount);
        details = C.Small("File details", () => { fileDetails.IsVisible = !fileDetails.IsVisible; UpdateFiles(); });
        View = C.Card(C.Stack(C.Space(heading, 8), C.Space(detail, 12), current, progress, C.Space(counts, 8), C.Space(error, 10), C.Space(buttons, 12), details, C.Space(fileDetails, 0, 12)));
        UpdateButtons();
        Update(record);
    }
    public void Update(TransferRecord next)
    {
        record = next;
        title.Text = record.FileCount + record.FolderCount > 1 ? $"{Count(record.FileCount, "file")} · {Count(record.FolderCount, "folder")}" : record.Title;
        status.Text = record.StatusText;
        status.Foreground = Theme.Brush(record.State == TransferState.Completed ? "Success" : record.State == TransferState.Failed ? "Danger" : "Primary");
        detail.Text = $"{(record.Direction == "Received" ? "From" : "To")} {record.PeerName} · LAN {record.PeerAddress} · {record.Transport}";
        current.Text = record.CurrentFile.Length > 0 ? record.CurrentFile : record.State == TransferState.AwaitingApproval ? "Waiting for the recipient to choose Accept or Decline." : "Preparing the transfer.";
        if (record.State == TransferState.Completed)
            current.Text = record.Transport.Contains("HTTP", StringComparison.Ordinal) ? record.Direction == "Received" ? "Saved on this computer · SHA-256 recorded locally" : "Sent to browser · saving is controlled by that device" : $"{Count(record.VerifiedFiles, "file")} verified · {Count(record.FolderCount, "folder")} · {Count(record.SkippedFiles, "file")} skipped";
        progress.Value = record.Progress;
        progress.Foreground = Theme.Brush(record.State == TransferState.Completed ? "Mint" : "Accent");
        amount.Text = record.ProgressText;
        speed.Text = record.SpeedText + (record.TimeLeft.Length > 0 ? " · " + record.TimeLeft : "");
        error.Text = record.Error;
        error.IsVisible = record.Error.Length > 0;
        details.Content = $"File details ({Math.Max(record.FileCount, record.Files.Count)})";
        if (fileDetails.IsVisible)
            UpdateFiles();
        if (lastTerminal != record.Terminal)
            UpdateButtons();
        lastTerminal = record.Terminal;
    }
    private static string Count(int value, string singular) => value + " " + singular + (value == 1 ? "" : "s");
    private void UpdateButtons()
    {
        buttons.Children.Clear();
        if (!record.Terminal)
            buttons.Children.Add(C.Small("Cancel", () => app.Engine.Transfers.Cancel(record.Id, record.Direction)));
        if (record.Terminal && record.Direction == "Sent" && record.State != TransferState.Completed && !record.Transport.Contains("HTTP", StringComparison.Ordinal))
            buttons.Children.Add(C.Small("Retry", () => { app.Engine.Retry(record); app.Tell("Retry queued. The recipient will be asked again."); }));
        if (record.Direction == "Received" && Directory.Exists(record.Destination))
            buttons.Children.Add(C.Small("Reveal receive folder", () => app.Reveal(record.Destination)));
        if (record.Terminal)
            buttons.Children.Add(C.Small("Delete", () => _ = DeleteAsync(app, record)));
        if (buttons is WrapPanel)
            foreach (Control button in buttons.Children)
                button.Margin = new(0, 0, 8, 8);
    }
    /// <summary>Removes one finished transfer from the list and history after a confirmation; received files stay.</summary>
    public static async Task DeleteAsync(Shell app, TransferRecord record)
    {
        if (!await app.Dialogs.ConfirmAsync("Delete transfer", "Delete this transfer from the list and history?\n\n" + record.Title + " · " + record.PeerName + "\n\nReceived files stay on this " + Shell.Platform + ".", "Delete"))
            return;
        if (app.Engine.Transfers.Remove(record.Id, record.Direction))
            app.Tell("Transfer deleted from the list. Received files were kept.");
        app.RefreshPage();
    }
    private void UpdateFiles()
    {
        fileDetails.Children.Clear();
        foreach (TransferFile file in record.Files.Take(200))
            fileDetails.Children.Add(C.Space(C.Text($"{file.RelativePath}\n{file.Status} · {Format.Bytes(file.Bytes)} / {Format.Bytes(file.Size)}", 11, "Muted"), 10));
        if (Math.Max(record.FileCount, record.Files.Count) > 200)
            fileDetails.Children.Add(C.Text("Showing the first 200 file details. The current file is shown above.", 11, "Muted"));
    }
}

internal sealed class HistoryPage : IPage
{
    private readonly Shell app;
    private readonly StackPanel list = new();
    public Control View
    {
        get;
    }
    public HistoryPage(Shell app)
    {
        this.app = app;
        View = C.Stack(C.Header("Manage", "History", "What moved, and when. File contents are never stored.", C.Inline(C.AsyncButton("Export log", ExportAsync), C.AsyncButton("Clear finished", ClearAsync))), C.Card(list, 18),
            C.Space(C.Text("Search by file name, person or device at the top. Clearing history never deletes received files.", 11, "Muted"), 0, 12));
    }
    private string shown = "";
    public void Refresh()
    {
        TransferRecord[] records = app.Engine.Transfers.Records.Where(r => r.Terminal && (r.Title.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.PeerName.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.Files.Any(f => f.RelativePath.Contains(app.Search, StringComparison.OrdinalIgnoreCase)))).Take(100).ToArray();
        // Transfers report progress several times a second; History is rebuilt only when its finished transfers change.
        string key = string.Join('|', records.Select(r => r.Id + r.Direction + r.State)) + "#" + app.Search + Theme.Dark;
        if (key == shown && list.Children.Count > 0)
            return;
        shown = key;
        list.Children.Clear();
        // A phone is too narrow for the seven columns (the names and Delete were squeezed off screen): each entry is two lines there instead.
        bool phone = OperatingSystem.IsAndroid();
        if (!phone)
            list.Children.Add(Row(C.Eyebrow("File"), C.Eyebrow("Device"), C.Eyebrow("Direction"), C.Eyebrow("Size"), C.Eyebrow("Status"), C.Eyebrow("When"), new Panel()));
        if (records.Length == 0)
            list.Children.Add(C.Space(C.Text("No matching history. Completed, declined, cancelled and failed transfers appear here.", 13, "Muted"), 12, 12));
        foreach (TransferRecord record in records)
        {
            string color = record.State == TransferState.Completed ? "Mint" : record.State == TransferState.Failed ? "Danger" : "Muted";
            Control reveal = record.Direction == "Received" && Directory.Exists(record.Destination) ? C.IconButton("folder", () => app.Reveal(record.Destination), "Show in folder", true) : new Panel();
            TextBlock device = C.Text(record.PeerName + (record.PeerAddress.Length > 0 ? " · " + record.PeerAddress : ""), 12, "Muted");
            device.TextTrimming = TextTrimming.CharacterEllipsis;
            device.TextWrapping = TextWrapping.NoWrap;
            Control actions = C.Inline(reveal, C.IconButton("close", () => _ = TransferRow.DeleteAsync(app, record), "Delete from history", true));
            if (phone)
            {
                list.Children.Add(new Border { BorderBrush = Theme.Brush("Text/6"), BorderThickness = new(0, 1, 0, 0), Padding = new(0, 10), Child = PhoneRow(record, color, actions) });
                continue;
            }
            Grid row = Row(C.Medium(record.Title, 13), device, C.Text(record.Direction, 12, "Muted"), C.Text(record.SizeText, 12, "Muted"), C.Badge(record.StatusText, color, color + "/14"), C.Text(record.CreatedUtc.ToLocalTime().ToString("dd MMM, HH:mm"), 12, "Muted"), actions);
            list.Children.Add(new Border { BorderBrush = Theme.Brush("Text/6"), BorderThickness = new(0, 1, 0, 0), Child = row });
        }
    }
    private static Control PhoneRow(TransferRecord record, string color, Control actions)
    {
        TextBlock title = C.Medium(record.Title, 14), detail = C.Text(string.Join(" · ", new[] { record.Direction, record.PeerName, record.SizeText, record.CreatedUtc.ToLocalTime().ToString("dd MMM, HH:mm") }.Where(part => part.Length > 0)), 12, "Muted");
        foreach (TextBlock text in new[] { title, detail })
        {
            text.TextTrimming = TextTrimming.CharacterEllipsis;
            text.TextWrapping = TextWrapping.NoWrap;
        }
        Control badge = C.Badge(record.StatusText, color, color + "/14");
        DockPanel top = new()
        {
            Margin = new(0, 0, 0, 6)
        }, bottom = new();
        DockPanel.SetDock(badge, Dock.Right);
        DockPanel.SetDock(actions, Dock.Right);
        badge.Margin = new(10, 0, 0, 0);
        actions.VerticalAlignment = detail.VerticalAlignment = VerticalAlignment.Center;
        top.Children.Add(badge);
        top.Children.Add(title);
        bottom.Children.Add(actions);
        bottom.Children.Add(detail);
        return C.Stack(top, bottom);
    }
    private static Grid Row(params Control[] cells)
    {
        Grid row = new()
        {
            ColumnDefinitions = new ColumnDefinitions("2*,1.5*,90,80,120,110,92"),
            MinHeight = 46
        };
        for (int index = 0; index < cells.Length; index++)
        {
            cells[index].VerticalAlignment = VerticalAlignment.Center;
            cells[index].Margin = new(0, 0, 12, 0);
            Grid.SetColumn(cells[index], index);
            row.Children.Add(cells[index]);
        }
        return row;
    }
    private async Task ClearAsync()
    {
        if (!await app.Dialogs.ConfirmAsync("Clear history", "Clear finished transfer metadata? Received files stay on disk. Active transfers remain listed.", "Clear"))
            return;
        app.Engine.Transfers.ClearFinished();
        Refresh();
        app.Tell("Finished history cleared. Received files were kept.");
    }
    private async Task ExportAsync()
    {
        IStorageFile? file = await app.Storage.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Export transfer metadata", SuggestedFileName = "hopo-history.csv", DefaultExtension = "csv" });
        if (file == null)
            return;
        StringBuilder csv = new("Time UTC,Direction,Person or device,Status,Files,Bytes,Verified files,Skipped files\r\n");
        foreach (TransferRecord r in app.Engine.Transfers.Records)
            csv.AppendLine(string.Join(',', new[] { r.CreatedUtc.ToString("O"), r.Direction, r.PeerName, r.State.ToString(), r.FileCount.ToString(), r.TotalBytes.ToString(), r.VerifiedFiles.ToString(), r.SkippedFiles.ToString() }.Select(Cell)));
        await using Stream stream = await file.OpenWriteAsync();
        await stream.WriteAsync(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());
        app.Tell("Exported metadata without file contents, paths, addresses, pairing codes, or keys.");
        // Leading formula characters are neutralized so a spreadsheet never evaluates a device name.
        static string Cell(string value)
        {
            if (value.Length > 0 && "=+-@".Contains(value[0]))
                value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
