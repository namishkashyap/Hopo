using System.Windows.Automation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Lantoss.UI;

internal sealed class Dialogs(AppShell app)
{
    private readonly SemaphoreSlim approvals = new(1, 1);
    private Window? pairingWindow;
    private Window Create(string title, double width = 560)
    {
        // With hopo hidden in the tray or minimised, an owned dialog had no taskbar button and stayed out of sight, so hopo looked frozen.
        // Such a dialog stands on its own: centred on the screen, in the taskbar, and brought to the front once.
        Window? owner = pairingWindow is { IsVisible: true } ? pairingWindow : app.Window.IsVisible && app.Window.WindowState != WindowState.Minimized ? app.Window : null;
        Window window = new()
        {
            Title = title,
            Width = width,
            SizeToContent = SizeToContent.Height,
            MaxHeight = Math.Max(500, Math.Min(850, SystemParameters.WorkArea.Height - 50)),
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            Owner = owner,
            ShowInTaskbar = owner == null,
            Topmost = owner == null,
            FontFamily = new("Segoe UI"),
            FontSize = 13
        };
        if (owner == null)
            window.ContentRendered += (_, _) => { window.Activate(); window.Topmost = false; };
        window.Resources.MergedDictionaries.Add(app.Window.Resources);
        window.SetResourceReference(Control.BackgroundProperty, "Background");
        window.SetResourceReference(Control.ForegroundProperty, "Text");
        return window;
    }
    public void Pair(Peer? candidate = null)
    {
        if (pairingWindow is { IsVisible: true })
        {
            pairingWindow.Activate();
            return;
        }
        if (!app.Engine.Running)
            app.ToggleReceiving();
        if (candidate == null)
        {
            app.Engine.RefreshDiscovery();
            app.Navigate("Devices");
            app.Tell("Choose the other PC below and click Connect. The new version connects directly unless code protection is enabled.");
        }
        else if (candidate.SupportsDirectConnect || candidate.SupportsQuickPair)
            ConnectNearby(candidate);
        else
            PairManually(candidate);
    }
    private void ConnectNearby(Peer peer)
    {
        Window window = Create("Connect to " + peer.Name);
        pairingWindow = window;
        using CancellationTokenSource work = new();
        TextBlock feedback = C.Text("Connecting directly to the other PC…", 13, "Muted");
        TextBox accessCode = new()
        {
            MaxLength = 24,
            FontSize = 20,
            Margin = new(0, 0, 0, 18),
            Visibility = peer.RequiresCode ? Visibility.Visible : Visibility.Collapsed
        };
        AutomationProperties.SetName(accessCode, "Optional access code required by the other PC");
        Button cancel = C.Button("Cancel", window.Close);
        Button retry = C.Button("Try again", () => { });
        retry.Visibility = peer.RequiresCode ? Visibility.Visible : Visibility.Collapsed;
        retry.Content = "Connect";
        retry.Margin = new(10, 0, 0, 0);
        StackPanel body = C.Stack(C.Space(C.Text("Connecting to " + peer.Name, 23, bold: true), 13),
            C.Space(C.Text(peer.RequiresCode ? "Access-code protection is enabled on that PC." : "Direct connection. No password or comparison code.", 14, "Primary", true), 17),
            C.Space(feedback, 18), accessCode, C.Space(C.Note("Files still need approval", "Connecting remembers this PC’s public key. The receiver reviews incoming files before they are saved."), 22), C.Inline(cancel, retry));
        body.Margin = new(28);
        window.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        async Task ConnectAsync()
        {
            retry.Visibility = Visibility.Collapsed;
            feedback.Text = "Connecting…";
            try
            {
                Peer result = peer.SupportsDirectConnect ? await app.Engine.ConnectDirectAsync(peer, accessCode.Text, work.Token, CompareAsync) : await app.Engine.PairNearbyAsync(peer, CompareAsync, work.Token);
                window.Close();
                app.Navigate("Devices");
                app.Tell("Connected to " + result.Name + ". Remembered for next time; choose Send when you are ready.");
            }
            catch (Exception error)
            {
                if (!work.IsCancellationRequested)
                {
                    feedback.Text = TransferEngine.HumanError(error);
                    if (error is CodeRequiredException)
                        accessCode.Visibility = Visibility.Visible;
                    retry.Visibility = Visibility.Visible;
                }
            }
        }
        retry.Click += async (_, _) => await ConnectAsync();
        window.Loaded += async (_, _) => { if (!peer.RequiresCode) await ConnectAsync(); else feedback.Text = "Enter its current code from Phone & browser, then click Connect."; };
        window.Closed += (_, _) => { work.Cancel(); pairingWindow = null; };
        window.ShowDialog();
    }
    public void PairManually(Peer? candidate = null)
    {
        if (pairingWindow is { IsVisible: true })
        {
            pairingWindow.Activate();
            return;
        }
        Window window = Create("Manual connection — fallback", 610);
        pairingWindow = window;
        using CancellationTokenSource work = new();
        TextBlock localAddress = C.Text(app.Engine.Endpoint, 15, "Text", true), localCode = C.Text(app.Engine.PairingCode.Value, 25, "Primary", true);
        localCode.FontFamily = new("Consolas");
        TextBlock expiry = C.Text("", 11, "Muted");
        Button receiving = C.Small(app.Engine.Running ? "Receiving is on" : "Start receiving", () => { if (!app.Engine.Running) app.ToggleReceiving(); });
        receiving.IsEnabled = !app.Engine.Running;
        TextBox address = new()
        {
            Text = candidate?.Endpoint ?? "",
            ToolTip = "Address shown by hopo on the other PC",
            Margin = new(0, 7, 0, 15)
        };
        AutomationProperties.SetName(address, "Other PC address and port");
        TextBox code = new()
        {
            FontFamily = new("Consolas"),
            FontSize = 20,
            MaxLength = 24,
            Margin = new(0, 7, 0, 15)
        };
        AutomationProperties.SetName(code, "Other PC pairing code");
        TextBlock feedback = C.Text("Both PCs need hopo. Browser pairing is not supported in this release.", 12, "Muted");
        Button pair = C.Button("Connect and compare codes", () => { }, true, "pair");
        pair.HorizontalAlignment = HorizontalAlignment.Stretch;
        pair.Click += async (_, _) =>
        {
            pair.IsEnabled = false;
            feedback.Text = "Connecting securely. Keep hopo open on the other PC…";
            try
            {
                Peer result = await app.Engine.PairAsync(address.Text, code.Text, CompareAsync, work.Token);
                app.Tell("Paired with " + result.Name + ". Every incoming transfer will still ask for approval.");
                window.Close();
                app.Navigate("Devices");
            }
            catch (Exception error) { feedback.Text = TransferEngine.HumanError(error); }
            finally { pair.IsEnabled = true; }
        };
        Button copy = C.Small("Copy my code", () => { Clipboard.SetText(app.Engine.PairingCode.Value); app.Tell("Pairing code copied. It expires after five minutes and one use."); });
        Button rotate = C.Small("New code", () => app.Engine.PairingCode.Rotate());
        rotate.Margin = new(8, 0, 0, 0);
        StackPanel local = C.Stack(C.Heading("This PC", receiving), C.Space(localAddress, 9), C.Space(localCode, 6), C.Space(expiry, 14), C.Inline(copy, rotate));
        StackPanel body = C.Stack(C.Space(C.Text("Connect when discovery is unavailable.", 21, bold: true), 10), C.Space(C.Text("Normally, choose the other PC in Devices and click Connect. Use these fields only if it does not appear, or the other PC has an older Lantoss version.", 12, "Muted"), 20), C.Space(C.Card(local), 22), C.Text("OTHER PC ADDRESS", 10, "Muted", true), address, C.Text("OTHER PC PAIRING CODE", 10, "Muted", true), code, C.Space(pair, 15), feedback);
        body.Margin = new(28);
        window.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += (_, _) => { localAddress.Text = app.Engine.Endpoint; localCode.Text = app.Engine.PairingCode.Value; expiry.Text = "Valid until " + app.Engine.PairingCode.ExpiresUtc.ToLocalTime().ToString("HH:mm") + " · single use"; receiving.Content = app.Engine.Running ? "Receiving is on" : "Start receiving"; receiving.IsEnabled = !app.Engine.Running; };
        timer.Start();
        window.Closed += (_, _) => { timer.Stop(); work.Cancel(); pairingWindow = null; };
        window.ShowDialog();
    }
    public async Task<bool> CompareAsync(PairRequest request, CancellationToken token)
    {
        await approvals.WaitAsync(token);
        try
        {
            return await app.Window.Dispatcher.InvokeAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                Window window = Create("Verify this connection");
                bool accepted = false;
                TextBlock comparison = C.Text(request.ComparisonCode, 30, "Primary", true);
                comparison.FontFamily = new("Consolas");
                comparison.TextAlignment = TextAlignment.Center;
                comparison.Margin = new(0, 22, 0, 22);
                Button decline = C.Button("Decline", window.Close), confirm = C.Button("Codes match — approve", () => { accepted = true; window.Close(); }, true);
                confirm.Margin = new(10, 0, 0, 0);
                TextBlock lookalike = C.Text(app.Store.NameInUse(request.Name, request.Fingerprint) ? "Another saved device already uses this name. Approve only if the codes match." : "", 12, "Danger", true);
                StackPanel body = C.Stack(C.Space(C.Text("Connect to " + request.Name + "?", 23, bold: true), 12), lookalike, C.Text("Compare this code with the other PC’s screen. Approve only if you expected this connection and the codes match exactly. You do not need to type it.", 13, "Muted"), comparison,
                    C.Space(C.Note("Both PCs must approve", "This step verifies the public keys, rather than trusting a device name or IP address."), 20), C.Space(C.Text("Peer public key: " + Format.ShortKey(request.Fingerprint), 11, "Muted"), 22), C.Inline(decline, confirm));
                body.Margin = new(28);
                window.Content = body;
                using CancellationTokenRegistration cancel = token.Register(() => window.Dispatcher.BeginInvoke(() => { if (window.IsVisible) window.Close(); }));
                window.ShowDialog();
                return accepted && !token.IsCancellationRequested;
            });
        }
        finally { approvals.Release(); }
    }
    public async Task<ReceiveDecision> ReceiveAsync(ReceiveRequest request, CancellationToken token)
    {
        await approvals.WaitAsync(token);
        try
        {
            return await app.Window.Dispatcher.InvokeAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                Window window = Create("Incoming file transfer", 640);
                bool accepted = false;
                string folder = request.DefaultFolder;
                TextBlock location = C.Text(folder, 11, "Muted"), free = C.Text("", 12, "Muted");
                ComboBox duplicates = new()
                {
                    ItemsSource = new[] { "Keep both (number duplicate names)", "Skip existing files", "Replace existing files (confirm)" },
                    SelectedIndex = 0,
                    Margin = new(0, 8, 0, 18)
                };
                duplicates.IsEnabled = !request.Browser && !request.FixedFolder;
                AutomationProperties.SetName(duplicates, "Duplicate file handling");
                Button accept = C.Button("Accept files", () =>
                {
                    if (duplicates.SelectedIndex == 2 && MessageBox.Show(window, "Replace existing files at matching paths? Only incoming files that pass integrity verification will replace originals.", "Confirm replacements", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                        return;
                    accepted = true;
                    window.Close();
                }, true, "download");
                void CheckSpace()
                {
                    try
                    {
                        long available = FileCatalog.FreeSpace(folder);
                        free.Text = Format.Bytes(available) + " available · " + Format.Bytes(request.Manifest.TotalBytes) + " requested";
                        accept.IsEnabled = available >= request.Manifest.TotalBytes + 64L * 1024 * 1024;
                    }
                    catch (IOException) { free.Text = "Destination drive is unavailable. Choose another folder."; accept.IsEnabled = false; }
                }
                Button choose = C.Small("Change folder", () => { OpenFolderDialog picker = new() { Title = "Choose the receive folder" }; if (picker.ShowDialog(window) == true) { FileCatalog.CheckSafeDirectory(picker.FolderName); folder = picker.FolderName; location.Text = folder; CheckSpace(); } });
                choose.IsEnabled = !request.FixedFolder;
                StackPanel names = C.Stack();
                foreach (ManifestEntry entry in request.Manifest.Entries.Take(8))
                    names.Children.Add(C.Space(C.Text((entry.Directory ? "Folder · " : "File · ") + entry.RelativePath, 12), 7));
                if (request.Manifest.Entries.Count > 8)
                    names.Children.Add(C.Text("+ " + (request.Manifest.Entries.Count - 8) + " more items", 11, "Muted"));
                Button decline = C.Button("Decline", window.Close);
                accept.Margin = new(10, 0, 0, 0);
                string sender = (request.Browser ? "Web browser" : "PC · key " + Format.ShortKey(request.Fingerprint)) + (request.Address.Length > 0 ? " · " + request.Address : "") + " · asked at " + DateTime.Now.ToString("HH:mm");
                // Warnings appear for programs and scripts, and for a PC whose name matches another saved device.
                StackPanel warnings = C.Stack();
                if (FileCatalog.Risky(request.Manifest))
                    warnings.Children.Add(C.Space(C.Text("Contains programs, scripts or shortcuts. Open them only if you trust the sender. Shortcut files are saved with .txt added.", 12, "Danger", true), 10));
                if (!request.Browser && app.Store.NameInUse(request.PeerName, request.Fingerprint))
                    warnings.Children.Add(C.Space(C.Text("Another saved device uses this name. Check the key before accepting.", 12, "Danger", true), 10));
                StackPanel body = C.Stack(C.Space(C.Text("Incoming files", 23, bold: true), 14), C.Space(C.Card(C.Person(request.PeerName, request.Picture, sender, 48), 16), 14),
                    C.Space(C.Text($"{Format.Count(request.Manifest.FileCount, "file")} · {Format.Count(request.Manifest.FolderCount, "folder")} · {Format.Bytes(request.Manifest.TotalBytes)}", 13, "Muted"), 18), warnings, C.Space(C.Card(names), 18),
                    C.Space(C.Badge(request.Browser ? "Phone/browser · approved on this PC" : request.KeyVerified ? "Paired device · public key verified" : "Connected PC · saved public key", "Success", "SuccessSoft"), 16), C.Space(C.Text("SAVE TO", 10, "Muted", true), 7), C.Space(location, 10), C.Space(choose, 10), C.Space(free, 18), C.Text("IF FILES ALREADY EXIST", 10, "Muted", true), duplicates,
                    C.Space(C.Text(request.FixedFolder ? "Files are saved into this shared folder and become visible to its connected devices. Existing files are kept. Browser transfers use HTTP; native PC transfers use TLS." : request.Browser ? "Browser uploads keep both copies. Files are saved atomically and never opened or shared automatically. Local HTTP is not encrypted." : "Files are checksum-verified before saving. Received files are never opened or shared automatically.", 11, "Muted"), 20), C.Inline(decline, accept));
                body.Margin = new(28);
                window.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                CheckSpace();
                using CancellationTokenRegistration cancel = token.Register(() => window.Dispatcher.BeginInvoke(() => { if (window.IsVisible) window.Close(); }));
                window.ShowDialog();
                return new ReceiveDecision(accepted && !token.IsCancellationRequested, folder, (DuplicatePolicy)duplicates.SelectedIndex);
            });
        }
        finally { approvals.Release(); }
    }
    /// <summary>Send to a folder: browses a paired PC's drop folders (names only) and sends <paramref name="paths"/> straight into the chosen one.</summary>
    public void SendToFolder(Peer peer, string[] paths)
    {
        Window window = Create("Send to a folder on " + peer.Name, 520);
        StackPanel list = new();
        TextBlock where = C.Text(peer.Name, 15, bold: true), status = C.Text("Reading folders…", 12, "Muted");
        where.TextTrimming = TextTrimming.CharacterEllipsis;
        status.TextWrapping = TextWrapping.Wrap;
        string folder = "";
        int load = 0;
        Button up = null!, send = null!;
        StackPanel newRow = null!;
        up = C.Button("Up one folder", () => _ = Open(folder.Contains('/') ? folder[..folder.LastIndexOf('/')] : ""), icon: "folder");
        send = C.Button("Send here", () => { string target = folder; window.Close(); _ = SendAsync(target); }, true, "send");
        up.IsEnabled = send.IsEnabled = false;
        send.Margin = new(10, 0, 0, 0);
        TextBox newName = new()
        {
            MinWidth = 200,
            Margin = new(0, 0, 8, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(newName, "New folder name");
        Button make = null!;
        make = C.Button("Create folder", () => _ = MakeFolder(), icon: "plus");
        Button paste = C.Button("Paste", () => { string text = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : ""; if (text.Length == 0) status.Text = "Nothing to paste."; else newName.Text = text; });
        paste.Margin = new(0, 0, 8, 0);
        newRow = C.Space(C.Stack(C.Space(C.Text("Make a new folder here. Use / for folders inside folders, for example Projects/2026.", 12, "Muted"), 5), C.Inline(newName, paste, make)), 14);
        newRow.Visibility = Visibility.Collapsed;
        newName.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) _ = MakeFolder(); };
        async Task MakeFolder()
        {
            string[] parts = newName.Text.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            try
            {
                if (parts.Length == 0)
                    throw new InvalidDataException();
                foreach (string part in parts)
                    FileCatalog.ValidateSegment(part);
            }
            catch (InvalidDataException) { status.Text = "Type a folder name without : * ? \" < > |, up to 180 characters."; return; }
            string path = folder + "/" + string.Join("/", parts);
            make.IsEnabled = false;
            status.Text = "Creating " + string.Join(" › ", parts) + " on " + peer.Name + "… It asks there first unless it trusts this PC.";
            try
            {
                if (await app.Engine.CreateFolderAsync(peer, path))
                {
                    newName.Text = "";
                    await Open(path);
                    status.Text = "Folder created. Send here, or make another one inside it.";
                }
                else
                    status.Text = peer.Name + " declined making the folder.";
            }
            catch (Exception error) { status.Text = TransferEngine.HumanError(error); }
            finally { make.IsEnabled = true; }
        }
        async Task Open(string path)
        {
            int mine = ++load;
            status.Text = "Reading folders…";
            try
            {
                SpaceEntry[] entries = await app.Engine.BrowseFoldersAsync(peer, path);
                if (mine != load)
                    return;
                folder = path;
                where.Text = path.Length == 0 ? peer.Name : peer.Name + " › " + path.Replace("/", " › ");
                up.IsEnabled = send.IsEnabled = path.Length > 0;
                newRow.Visibility = path.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                list.Children.Clear();
                status.Text = path.Length == 0 ? "Choose a folder. Only folder names are shown, never files." : entries.Length == 0 ? "No subfolders here. Send here, make a new folder, or go up." : "Open a subfolder, make a new one, or send here.";
                foreach (SpaceEntry entry in entries)
                {
                    Button row = C.Button(entry.Name, () => _ = Open(entry.Path), icon: "folder");
                    row.HorizontalAlignment = HorizontalAlignment.Stretch;
                    row.HorizontalContentAlignment = HorizontalAlignment.Left;
                    row.Margin = new(0, 0, 0, 6);
                    list.Children.Add(row);
                }
            }
            catch (Exception error) { if (mine == load) status.Text = TransferEngine.HumanError(error); }
        }
        async Task SendAsync(string target)
        {
            app.Navigate("Transfers");
            app.Tell("Sending " + Format.Count(paths.Length, "item") + " to " + target + " on " + peer.Name + ". It asks there first unless it trusts this PC.");
            try
            {
                await app.Engine.SendToFolderAsync(peer, target, paths);
                app.Tell("Finished sending to " + target + " on " + peer.Name + ". See Transfers for the result.");
            }
            catch (Exception error) { app.Error(error); }
        }
        StackPanel body = C.Stack(C.Space(C.Text(Format.Count(paths.Length, "item") + " to send", 12, "Muted"), 6), C.Space(where, 6), C.Space(status, 14), newRow, C.Space(new ScrollViewer { Content = list, MaxHeight = 360, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, 16), C.Inline(up, send));
        body.Margin = new(28);
        window.Content = body;
        window.Show();
        _ = Open("");
    }
    public async Task<bool> SyncAsync(SyncApproval request, CancellationToken token)
    {
        await approvals.WaitAsync(token);
        try
        {
            return await app.Window.Dispatcher.InvokeAsync(() =>
            {
                token.ThrowIfCancellationRequested();
                Window window = Create("Allow automatic folder sync?", 610);
                bool accepted = false;
                Button decline = C.Button("Decline", window.Close);
                Button approve = C.Button("Allow automatic sync", () => { accepted = true; window.Close(); }, true, "sync");
                approve.Margin = new(10, 0, 0, 0);
                StackPanel body = C.Stack(C.Space(C.Text(request.PeerName + " wants to sync " + request.Space.Name, 22, bold: true), 15),
                    C.Space(C.Text(request.Space.Folder, 12, "Muted"), 17), C.Space(C.Text("This grants the connected PC ongoing permission to add and update files in this shared folder while both apps are open. Individual sync changes will not ask again. Conflicts keep both copies; replaced versions have local backups. Deletions do not propagate.", 13), 20),
                    C.Space(C.Note("You can stop this later", "Revoke the link in Sync, change the space to Read only, stop sharing, or block the PC. Existing saved copies stay on the devices."), 22), C.Inline(decline, approve));
                body.Margin = new(28);
                window.Content = body;
                using CancellationTokenRegistration cancel = token.Register(() => window.Dispatcher.BeginInvoke(() => { if (window.IsVisible) window.Close(); }));
                window.ShowDialog();
                return accepted && !token.IsCancellationRequested;
            });
        }
        finally { approvals.Release(); }
    }
}
