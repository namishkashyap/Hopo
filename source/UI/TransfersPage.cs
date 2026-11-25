using System.Windows.Automation;

namespace Lantoss.UI;

internal sealed class TransfersPage : IPage
{
    private readonly AppShell app;
    private readonly StackPanel rows = new();
    private readonly Dictionary<string, TransferRow> cards = [];
    private readonly Dictionary<string, Button> filters = [];
    private string filter = "All", layoutKey = "";
    public FrameworkElement View
    {
        get;
    }
    public TransfersPage(AppShell app)
    {
        this.app = app;
        StackPanel tabs = new()
        {
            Orientation = Orientation.Horizontal
        };
        foreach (string name in new[] { "All", "Active", "Queued", "Completed", "Failed" })
        {
            Button button = C.Small(name, () => { filter = name; layoutKey = ""; Refresh(); });
            button.Margin = new(0, 0, 8, 0);
            filters[name] = button;
            tabs.Children.Add(button);
        }
        View = C.Stack(C.Hero("Manage", "Transfers", "Live progress, verified on arrival."), C.Space(tabs, 20), rows,
            C.Space(C.Text("Pause/resume and restart continuation are planned. Retry starts a new transfer and asks the recipient for approval again.", 11, "Muted"), 0, 20));
    }
    private string shownFilter = "";
    public void Refresh()
    {
        if (shownFilter != filter)
        {
            shownFilter = filter;
            foreach ((string name, Button button) in filters)
                button.SetResourceReference(Control.BackgroundProperty, name == filter ? "PrimarySoft" : "Surface");
        }
        TransferRecord[] records = app.Engine.Transfers.Records.Where(r => Matches(r) && (r.Title.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.PeerName.Contains(app.Search, StringComparison.OrdinalIgnoreCase) || r.Files.Any(f => f.RelativePath.Contains(app.Search, StringComparison.OrdinalIgnoreCase)))).Take(50).ToArray();
        string key = string.Join('|', records.Select(r => r.Id + r.Direction));
        if (layoutKey != key || rows.Children.Count == 0)
        {
            // A new or removed transfer reorders the list; cards that are still listed are kept (with their open file details and
            // buttons under the pointer) and only new transfers get a new card.
            layoutKey = key;
            rows.Children.Clear();
            Dictionary<string, TransferRow> kept = [];
            if (records.Length == 0)
                rows.Children.Add(C.Empty("Nothing in this queue.", "Send files from the Send screen, or approve an incoming transfer. Active and finished jobs appear here.", "transfer", C.Button("Choose files", app.PickFiles, icon: "plus")));
            foreach (TransferRecord record in records)
            {
                string id = record.Id + record.Direction;
                if (!cards.TryGetValue(id, out TransferRow? row))
                    row = new(app, record);
                kept[id] = row;
                rows.Children.Add(C.Space(row.View, 14));
            }
            cards.Clear();
            foreach ((string id, TransferRow row) in kept)
                cards[id] = row;
        }
        foreach (TransferRecord record in records)
        {
            if (cards.TryGetValue(record.Id + record.Direction, out TransferRow? row))
                row.Update(record);
        }
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
    private readonly AppShell app;
    private TransferRecord record;
    private readonly TextBlock title = C.Text("", 15, bold: true), status = C.Text("", 12, "Primary", true), detail = C.Text("", 11, "Muted"), current = C.Text("", 12), amount = C.Text("", 11, "Muted"), speed = C.Text("", 11, "Primary"), error = C.Text("", 12, "Danger");
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, Margin = new(0, 12, 0, 12) };
    private readonly StackPanel buttons = new() { Orientation = Orientation.Horizontal }, fileDetails = new();
    private readonly Expander expansion;
    private bool lastTerminal;
    public Border View
    {
        get;
    }
    public TransferRow(AppShell app, TransferRecord record)
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
        expansion = new()
        {
            Header = "File details",
            Content = fileDetails,
            Margin = new(0, 12, 0, 0)
        };
        expansion.SetResourceReference(Control.ForegroundProperty, "Muted");
        expansion.Expanded += (_, _) => UpdateFiles();
        View = C.Card(C.Stack(C.Space(heading, 8), C.Space(detail, 12), current, progress, C.Space(counts, 8), C.Space(error, 10), buttons, expansion));
        UpdateButtons();
        Update(record);
    }
    private TransferState? shownState;
    public void Update(TransferRecord next)
    {
        // An unchanged record (most refreshes, for finished transfers) costs nothing.
        if (ReferenceEquals(next, record) && shownState != null)
            return;
        record = next;
        title.Text = record.FileCount + record.FolderCount > 1 ? $"{Count(record.FileCount, "file")} · {Count(record.FolderCount, "folder")}" : record.Title;
        status.Text = record.StatusText;
        bool colours = shownState != record.State;
        shownState = record.State;
        if (colours)
        {
            status.SetResourceReference(TextBlock.ForegroundProperty, record.State == TransferState.Completed ? "Success" : record.State == TransferState.Failed ? "Danger" : "Primary");
            progress.SetResourceReference(Control.ForegroundProperty, record.State == TransferState.Completed ? "Success" : "Primary");
        }
        detail.Text = $"{record.Direction} · {record.PeerName} · LAN {record.PeerAddress} · {record.Transport}";
        current.Text = record.CurrentFile.Length > 0 ? record.CurrentFile : record.State == TransferState.AwaitingApproval ? "Waiting for the recipient to choose Accept or Decline." : "Preparing the transfer.";
        if (record.State == TransferState.Completed)
            current.Text = record.Transport.Contains("HTTP", StringComparison.Ordinal) ? record.Direction == "Received" ? "Saved on this PC · SHA-256 recorded locally" : "Sent to browser · browser saving is controlled by that device" : $"{Count(record.VerifiedFiles, "file")} verified · {Count(record.FolderCount, "folder")} · {Count(record.SkippedFiles, "file")} skipped";
        progress.Value = record.Progress;
        AutomationProperties.SetName(progress, "Transfer progress: " + record.ProgressText);
        amount.Text = record.ProgressText;
        speed.Text = record.SpeedText + (record.TimeLeft.Length > 0 ? " · " + record.TimeLeft : "");
        // Items left out of a send are listed with their reasons, so a skipped file is never a silent surprise.
        string notSent = record.NotSent.Count == 0 ? "" : $"Not sent ({record.NotSent.Count}): " + string.Join("\n", record.NotSent.Take(5)) + (record.NotSent.Count > 5 ? "\n…" : "");
        error.Text = string.Join("\n", new[] { record.Error, notSent }.Where(text => text.Length > 0));
        error.Visibility = error.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        expansion.Header = $"File details ({Math.Max(record.FileCount, record.Files.Count)})";
        if (expansion.IsExpanded)
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
        // The folder is checked when clicked, not while drawing: an unplugged or network drive would freeze the window.
        if (record.Direction == "Received" && record.Destination.Length > 0)
            buttons.Children.Add(C.Small("Reveal receive folder", () => app.Reveal(record.Destination)));
        if (record.Terminal && app.Admin)
            buttons.Children.Add(C.Small("Delete", () => Delete(app, record)));
        for (int i = 1; i < buttons.Children.Count; i++)
            ((FrameworkElement)buttons.Children[i]).Margin = new(8, 0, 0, 0);
    }
    /// <summary>hopo Admin removes one finished transfer from the list and history; received files stay on disk.</summary>
    public static void Delete(AppShell app, TransferRecord record)
    {
        if (MessageBox.Show(app.Window, "Delete this transfer from the list and history?\n\n" + record.Title + " · " + record.PeerName + "\n\nReceived files stay on this PC.", "Delete transfer", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        if (app.Engine.Transfers.Remove(record.Id, record.Direction))
            app.Tell("Transfer deleted from the list. Received files were kept.");
        app.RefreshPage();
    }
    private void UpdateFiles()
    {
        // The rows are made once and their text updated in place; rebuilding 200 rows four times a second froze the window.
        TransferFile[] files = [.. record.Files.Take(200)];
        bool more = Math.Max(record.FileCount, record.Files.Count) > 200;
        if (fileDetails.Children.Count != files.Length + (more ? 1 : 0))
        {
            fileDetails.Children.Clear();
            foreach (TransferFile _ in files)
                fileDetails.Children.Add(C.Space(C.Text("", 11, "Muted"), 10));
            if (more)
                fileDetails.Children.Add(C.Text("Showing the first 200 file details. The current file is shown above.", 11, "Muted"));
        }
        for (int i = 0; i < files.Length; i++)
        {
            string text = $"{files[i].RelativePath}\n{files[i].Status} · {Format.Bytes(files[i].Bytes)} / {Format.Bytes(files[i].Size)}";
            TextBlock row = (TextBlock)fileDetails.Children[i];
            if (row.Text != text)
                row.Text = text;
        }
    }
}
