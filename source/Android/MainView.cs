using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Application = Avalonia.Application;
using Button = Avalonia.Controls.Button;
using Orientation = Avalonia.Layout.Orientation;
using ProgressBar = Avalonia.Controls.ProgressBar;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Lantoss.Core;
using Lantoss.Networking;

namespace Lantoss.Android;

/// <summary>What the Android layer provides to the shared screen: notifications and the Downloads folder.</summary>
internal interface IPhonePlatform
{
    void Notify(string title, string text);
    /// <summary>Copies every file under <paramref name="folder"/> into Downloads/hopo and returns how many were saved.</summary>
    int SaveToDownloads(string folder);
}

/// <summary>The whole phone app on one scrolling screen: status, your PCs, nearby PCs, transfers and text. Dialogs open over it.</summary>
internal sealed class MainView : UserControl
{
    public static MainView? Current
    {
        get; private set;
    }
    public static IPhonePlatform? Platform
    {
        get; set;
    }
    private static readonly Dictionary<string, string> outgoing = [];
    private readonly StackPanel content = new() { Spacing = 14, Margin = new(16, 20, 16, 28) };
    private readonly Panel overlay = new() { IsVisible = false, Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)) };
    private readonly TextBlock toast = new() { TextWrapping = TextWrapping.Wrap, Margin = new(16, 0, 16, 20), VerticalAlignment = VerticalAlignment.Bottom, IsVisible = false, Padding = new(14, 10), Background = new SolidColorBrush(Color.Parse("#E6222230")), Foreground = Brushes.White };
    private readonly SemaphoreSlim dialogs = new(1, 1);
    private readonly DispatcherTimer refresh = new() { Interval = TimeSpan.FromMilliseconds(400) }, toastTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    private bool dirty = true;
    private string? startMessage;
    private static TransferEngine Engine => HopoHost.Engine;
    public MainView()
    {
        Current = this;
        Grid root = new();
        root.Children.Add(new ScrollViewer { Content = content });
        root.Children.Add(new Border { Child = toast, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false });
        root.Children.Add(overlay);
        Content = root;
        Engine.ApprovePair = CompareAsync;
        Engine.ApproveReceive = ReceiveAsync;
        Engine.ApproveSync = (_, _) => { Tell("Folder sync is not available on Android yet."); return Task.FromResult(false); };
        Engine.PeersChanged += MarkDirty;
        Engine.Transfers.Changed += OnTransfer;
        Engine.Browser.Notes.Changed += MarkDirty;
        Engine.Status += message => Dispatcher.UIThread.Post(() => Tell(message));
        Engine.Completed += OnCompleted;
        HopoHost.Started += MarkDirty;
        // Pages are rebuilt at most a few times a second, however many progress updates arrive.
        refresh.Tick += (_, _) => { if (dirty) Refresh(); };
        toastTimer.Tick += (_, _) => { toast.IsVisible = false; toastTimer.Stop(); };
        AttachedToVisualTree += (_, _) => { refresh.Start(); startMessage = HopoHost.StartReceiving(); Refresh(); };
        DetachedFromVisualTree += (_, _) => refresh.Stop();
    }
    private void MarkDirty() => dirty = true;
    private void OnTransfer(TransferRecord record)
    {
        dirty = true;
        // Copies made for sending are removed once the transfer ends, however it ends.
        if (record.Direction == "Sent" && record.Terminal)
            lock (outgoing)
                if (outgoing.Remove(record.Id, out string? folder))
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
    private void OnCompleted(TransferRecord record)
    {
        if (record.Direction != "Received" || record.State != TransferState.Completed)
            return;
        string folder = Path.Combine(HopoHost.Inbox, record.Id);
        if (!Directory.Exists(folder))
            return;
        int saved = Platform?.SaveToDownloads(folder) ?? 0;
        string text = saved > 0 ? $"{saved} {(saved == 1 ? "file" : "files")} from {record.PeerName} saved to Downloads/hopo." : $"Files from {record.PeerName} received.";
        Platform?.Notify("Files received", text);
        Dispatcher.UIThread.Post(() => Tell(text));
    }
    public void Tell(string message)
    {
        toast.Text = message;
        toast.IsVisible = true;
        toastTimer.Stop();
        toastTimer.Start();
    }
    // ---------- Screen ----------
    private void Refresh()
    {
        dirty = false;
        content.Children.Clear();
        Peer[] peers = Engine.Peers;
        content.Children.Add(Header());
        Peer[] paired = peers.Where(p => p.Paired && !p.Blocked).OrderByDescending(p => p.Online).ThenBy(p => p.Name).ToArray();
        StackPanel yours = Section("Your PCs");
        if (paired.Length == 0)
            yours.Children.Add(Muted("Open hopo on your PC. It appears under Nearby; tap Connect and check that both screens show the same code."));
        foreach (Peer peer in paired)
        {
            StackPanel actions = Row(Button("Send files", () => SendFilesAsync(peer), true), Button("Send text", () => SendTextAsync(peer)));
            yours.Children.Add(Card(Title(peer.Name), Muted(peer.Online ? "Online · " + peer.Address : "Offline · last seen " + peer.LastSeenUtc.ToLocalTime().ToString("d MMM HH:mm")), actions));
        }
        content.Children.Add(yours);
        Peer[] nearby = peers.Where(p => !p.Paired && !p.Blocked && p.Online).OrderBy(p => p.Name).ToArray();
        StackPanel near = Section("Nearby");
        if (nearby.Length == 0)
            near.Children.Add(Muted(Engine.Running ? "Looking for PCs with hopo open on this Wi-Fi…" : "Start receiving to find PCs on this Wi-Fi."));
        foreach (Peer peer in nearby)
            near.Children.Add(Card(Title(peer.Name), Muted(peer.Address), Row(Button("Connect", () => ConnectAsync(peer), true))));
        content.Children.Add(near);
        TransferRecord[] records = Engine.Transfers.Records.Take(8).ToArray();
        if (records.Length > 0)
        {
            StackPanel transfers = Section("Transfers");
            foreach (TransferRecord record in records)
            {
                ProgressBar bar = new()
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = record.Progress,
                    Height = 6,
                    IsVisible = !record.Terminal
                };
                List<Control> parts = [Title((record.Direction == "Sent" ? "To " : "From ") + record.PeerName + " · " + record.Title), Muted($"{record.StatusText} · {record.ProgressText}"), bar];
                if (!record.Terminal)
                    parts.Add(Row(Button("Cancel", () => { Engine.Transfers.Cancel(record.Id, record.Direction); return Task.CompletedTask; })));
                transfers.Children.Add(Card([.. parts]));
            }
            content.Children.Add(transfers);
        }
        SharedNote[] notes = Engine.Browser.Notes.Snapshot().Notes.Take(10).ToArray();
        if (notes.Length > 0)
        {
            StackPanel text = Section("Text & links");
            foreach (SharedNote note in notes)
                text.Children.Add(Card(new TextBlock { Text = note.Content, TextWrapping = TextWrapping.Wrap, MaxLines = 6, TextTrimming = TextTrimming.CharacterEllipsis }, Muted("From " + note.Sender), Row(Button("Copy", () => CopyAsync(note.Content)))));
            content.Children.Add(text);
        }
    }
    private Control Header()
    {
        string status = Engine.Running && Engine.Adapter != null ? "Ready on Wi-Fi · " + Engine.Adapter.Address : startMessage ?? "Not receiving.";
        StackPanel header = new()
        {
            Spacing = 4
        };
        header.Children.Add(new TextBlock { Text = "hopo", FontSize = 30, FontWeight = FontWeight.Bold });
        header.Children.Add(Title(HopoHost.Store.Preferences.DeviceName));
        header.Children.Add(Muted(status));
        if (!Engine.Running)
            header.Children.Add(Row(Button("Start receiving", () => { startMessage = HopoHost.StartReceiving(); MarkDirty(); return Task.CompletedTask; }, true)));
        return header;
    }
    // ---------- Actions ----------
    private async Task ConnectAsync(Peer peer)
    {
        string code = "";
        if (peer.RequiresCode)
        {
            if (await InputAsync("Access code", "That PC asks for its access code. Read it in hopo on the PC.", "Connect") is not { Length: > 0 } typed)
                return;
            code = typed;
        }
        try
        {
            Tell("Connecting to " + peer.Name + "…");
            Peer paired = peer.SupportsDirectConnect ? await Engine.ConnectDirectAsync(peer, code, default, CompareAsync) : await Engine.PairNearbyAsync(peer, CompareAsync);
            Tell("Connected to " + paired.Name + ".");
        }
        catch (Exception error) { Tell(TransferEngine.HumanError(error)); }
        MarkDirty();
    }
    private string lastInput = "";
    private async Task SendFilesAsync(Peer peer)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
            return;
        IReadOnlyList<IStorageFile> picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Send to " + peer.Name, AllowMultiple = true });
        if (picked.Count == 0)
            return;
        string folder = Path.Combine(HopoHost.Outgoing, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        List<string> paths = [];
        try
        {
            Tell($"Preparing {picked.Count} {(picked.Count == 1 ? "file" : "files")}…");
            foreach (IStorageFile file in picked)
                paths.Add(await CopyInAsync(file, folder));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Tell("A chosen file could not be read on this phone. " + error.Message);
            Directory.Delete(folder, true);
            return;
        }
        Send(peer, paths, folder);
    }
    private static async Task<string> CopyInAsync(IStorageFile file, string folder)
    {
        string name = Unique(folder, SafeName(file.Name));
        await using Stream source = await file.OpenReadAsync();
        await using FileStream target = File.Create(name);
        await source.CopyToAsync(target);
        return name;
    }
    /// <summary>Phone apps can hand over names the PC would refuse (reserved or too long); the copy gets a safe name.</summary>
    public static string SafeName(string name)
    {
        string clean = new string(name.Where(c => !FileCatalog.Hidden(c) && Array.IndexOf(Path.GetInvalidFileNameChars(), c) < 0 && c is not ('<' or '>' or ':' or '"' or '|' or '?' or '*' or '\\')).ToArray()).Trim().TrimEnd('.');
        if (clean.Length > 120)
            clean = clean[..(120 - Path.GetExtension(clean).Length)] + Path.GetExtension(clean);
        try
        {
            FileCatalog.ValidateSegment(clean);
        }
        catch (InvalidDataException) { clean = "file " + DateTime.Now.ToString("yyyyMMdd-HHmmss") + Path.GetExtension(clean); }
        return clean.Length > 0 ? clean : "shared file";
    }
    private static string Unique(string folder, string name)
    {
        string path = Path.Combine(folder, name);
        for (int i = 2; File.Exists(path); i++)
            path = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}");
        return path;
    }
    private void Send(Peer peer, List<string> paths, string folder)
    {
        try
        {
            TransferRecord record = Engine.Send(peer, paths);
            lock (outgoing)
                outgoing[record.Id] = folder;
            Tell($"Sending to {peer.Name}. The PC asks its user to accept.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException)
        {
            Tell(TransferEngine.HumanError(error));
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException) { }
        }
        MarkDirty();
    }
    private async Task SendTextAsync(Peer peer, string? prefill = null)
    {
        if (await InputAsync("Send text to " + peer.Name, "Type or paste a note or link.", "Send", prefill, true) is not { Length: > 0 } text)
            return;
        try
        {
            await Engine.SendNoteAsync(peer, text);
            Tell("Sent to " + peer.Name + ".");
        }
        catch (Exception error) { Tell(TransferEngine.HumanError(error)); }
    }
    private async Task CopyAsync(string text)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(text);
        Tell("Copied.");
    }
    /// <summary>Files or text shared to hopo from another app: choose a PC, then send.</summary>
    public async Task ShareAsync(string[] paths, string folder, string? text)
    {
        if (await ChoosePeerAsync(paths.Length > 0 ? $"Send {paths.Length} {(paths.Length == 1 ? "file" : "files")} to" : "Send text to") is not Peer peer)
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException) { }
            return;
        }
        if (paths.Length > 0)
            Send(peer, [.. paths], folder);
        if (!string.IsNullOrWhiteSpace(text))
            await SendTextAsync(peer, text);
    }
    // ---------- Approvals (called by the engine) ----------
    private Task<bool> CompareAsync(PairRequest request, CancellationToken token)
        => Dispatcher.UIThread.InvokeAsync(() => ConfirmAsync("Connect to " + request.Name + "?", "Check that the PC shows the same code. If it does not match, choose Cancel.", "Codes match", "Cancel", request.ComparisonCode, token));
    private async Task<ReceiveDecision> ReceiveAsync(ReceiveRequest request, CancellationToken token)
    {
        string what = $"{request.Manifest.FileCount} {(request.Manifest.FileCount == 1 ? "file" : "files")} · {Format.Bytes(request.Manifest.Entries.Sum(e => e.Size))}";
        Platform?.Notify(request.PeerName + " wants to send files", what + ". Open hopo to accept.");
        bool accepted = await Dispatcher.UIThread.InvokeAsync(() => ConfirmAsync(request.PeerName + " wants to send", what + (request.KeyVerified ? "" : "\nThis PC connected without a code check."), "Accept", "Decline", null, token));
        if (!accepted)
            return new(false, "");
        string folder = Path.Combine(HopoHost.Inbox, request.Manifest.Id);
        Directory.CreateDirectory(folder);
        return new(true, folder);
    }
    // ---------- Dialogs ----------
    private async Task<bool> ConfirmAsync(string title, string body, string yes, string no, string? code, CancellationToken token)
    {
        TaskCompletionSource<bool> answer = new();
        List<Control> parts = [Title(title), new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap }];
        if (code != null)
            parts.Add(new TextBlock { Text = code, FontSize = 30, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 8), FontFamily = new FontFamily("monospace") });
        parts.Add(Row(Button(yes, () => { answer.TrySetResult(true); return Task.CompletedTask; }, true), Button(no, () => { answer.TrySetResult(false); return Task.CompletedTask; })));
        return await ShowAsync(parts, answer, token);
    }
    private async Task<string?> InputAsync(string title, string hint, string ok, string? prefill = null, bool multiline = false)
    {
        TaskCompletionSource<bool> answer = new();
        TextBox box = new()
        {
            Text = prefill ?? "",
            PlaceholderText = hint,
            AcceptsReturn = multiline,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = multiline ? 100 : 0,
            MaxLength = SharedNotes.MaxCharacters
        };
        List<Control> parts = [Title(title), box, Row(Button(ok, () => { answer.TrySetResult(true); return Task.CompletedTask; }, true), Button("Cancel", () => { answer.TrySetResult(false); return Task.CompletedTask; }))];
        bool sent = await ShowAsync(parts, answer, default, box);
        lastInput = (box.Text ?? "").Trim();
        return sent && lastInput.Length > 0 ? lastInput : null;
    }
    private async Task<Peer?> ChoosePeerAsync(string title)
    {
        Peer[] paired = Engine.Peers.Where(p => p.Paired && !p.Blocked).OrderByDescending(p => p.Online).ThenBy(p => p.Name).ToArray();
        if (paired.Length == 0)
        {
            Tell("Connect a PC first: open hopo on the PC, then tap Connect under Nearby.");
            return null;
        }
        TaskCompletionSource<bool> answer = new();
        Peer? chosen = null;
        List<Control> parts = [Title(title)];
        foreach (Peer peer in paired)
            parts.Add(Button(peer.Name + (peer.Online ? "" : " (offline)"), () => { chosen = peer; answer.TrySetResult(true); return Task.CompletedTask; }, peer.Online));
        parts.Add(Button("Cancel", () => { answer.TrySetResult(false); return Task.CompletedTask; }));
        return await ShowAsync(parts, answer, default) ? chosen : null;
    }
    private async Task<bool> ShowAsync(List<Control> parts, TaskCompletionSource<bool> answer, CancellationToken token, Control? focus = null)
    {
        await dialogs.WaitAsync(token);
        try
        {
            StackPanel stack = new()
            {
                Spacing = 12
            };
            foreach (Control part in parts)
                stack.Children.Add(part);
            overlay.Children.Clear();
            overlay.Children.Add(new Border { Child = stack, Margin = new(20), Padding = new(20), CornerRadius = new(18), MaxWidth = 440, VerticalAlignment = VerticalAlignment.Center, Background = Surface });
            overlay.IsVisible = true;
            focus?.Focus();
            using CancellationTokenRegistration cancel = token.Register(() => answer.TrySetResult(false));
            return await answer.Task;
        }
        finally
        {
            overlay.IsVisible = false;
            overlay.Children.Clear();
            dialogs.Release();
        }
    }
    // ---------- Building blocks ----------
    private static IBrush Surface => Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark ? new SolidColorBrush(Color.Parse("#1E1D26")) : Brushes.White;
    private static StackPanel Section(string title)
    {
        StackPanel section = new()
        {
            Spacing = 10
        };
        section.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.SemiBold, Margin = new(0, 8, 0, 0) });
        return section;
    }
    private static Border Card(params Control[] parts)
    {
        StackPanel stack = new()
        {
            Spacing = 6
        };
        foreach (Control part in parts)
            stack.Children.Add(part);
        return new Border { Child = stack, Padding = new(14), CornerRadius = new(14), Background = new SolidColorBrush(Color.FromArgb(28, 128, 128, 160)) };
    }
    private static TextBlock Title(string text) => new() { Text = text, FontSize = 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private static TextBlock Muted(string text) => new() { Text = text, FontSize = 13, Opacity = 0.72, TextWrapping = TextWrapping.Wrap };
    private static StackPanel Row(params Control[] items)
    {
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new(0, 4, 0, 0)
        };
        foreach (Control item in items)
            row.Children.Add(item);
        return row;
    }
    private Button Button(string text, Func<Task> action, bool primary = false)
    {
        Button button = new()
        {
            Content = text,
            MinHeight = 44,
            Padding = new(16, 8),
            CornerRadius = new(12)
        };
        if (primary)
            button.Classes.Add("accent");
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            try
            {
                await action();
            }
            catch (Exception error) { Tell(TransferEngine.HumanError(error)); }
            finally { button.IsEnabled = true; }
        };
        return button;
    }
}
