using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Ellipse = System.Windows.Shapes.Ellipse;
using Shape = System.Windows.Shapes.Shape;

namespace Lantoss.UI;

/// <summary>
/// Watches for the send shortcut on its own thread, so a busy window never delays typing anywhere. Only one running hopo app watches;
/// the other takes over when it closes. Keys are only taken when <see cref="SendShortcut.Matches"/> says so; everything else passes through.
/// </summary>
internal sealed class ShortcutWatcher : IDisposable
{
    private const int KeyboardHook = 13, KeyDown = 0x100, SystemKeyDown = 0x104, Quit = 0x12;
    private readonly Action<IntPtr> pressed;
    private readonly ManualResetEvent stop = new(false);
    private readonly HookProc callback;
    private volatile string setting;
    private int thread;
    public ShortcutWatcher(string setting, Action<IntPtr> pressed)
    {
        this.setting = setting;
        this.pressed = pressed;
        callback = Hook;
        new Thread(Run) { IsBackground = true, Name = "hopo send shortcut" }.Start();
    }
    public string Setting
    {
        set => setting = value;
    }
    private void Run()
    {
        using Mutex owner = new(false, @"Local\hopo.SendShortcut");
        try
        {
            if (WaitHandle.WaitAny([owner, stop]) == 1)
                return;
        }
        catch (AbandonedMutexException) { }
        try
        {
            // A message queue must exist before Dispose can post Quit to it.
            _ = PeekMessage(out Message message, IntPtr.Zero, 0, 0, 0);
            Volatile.Write(ref thread, (int)GetCurrentThreadId());
            IntPtr hook = SetWindowsHookEx(KeyboardHook, callback, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero)
                return;
            try
            {
                while (!stop.WaitOne(0) && GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
                {
                }
            }
            finally { UnhookWindowsHookEx(hook); }
        }
        finally { owner.ReleaseMutex(); }
    }
    private IntPtr Hook(int code, IntPtr message, IntPtr data)
    {
        // Only S is looked at; the window checks are a few quick system calls.
        if (code >= 0 && (message == KeyDown || message == SystemKeyDown) && Marshal.ReadInt32(data) == 0x53 && Opens(out IntPtr window))
        {
            pressed(window);
            return 1;
        }
        return CallNextHookEx(IntPtr.Zero, code, message, data);
    }
    private bool Opens(out IntPtr window)
    {
        window = GetForegroundWindow();
        static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
        GuiThreadInfo info = new()
        {
            Size = Marshal.SizeOf<GuiThreadInfo>()
        };
        if (window == IntPtr.Zero || !GetGUIThreadInfo(GetWindowThreadProcessId(window, out _), ref info) || info.Focus == IntPtr.Zero)
            return false;
        return SendShortcut.Matches(setting, 0x53, Down(0x11), Down(0x10), Down(0x12), Down(0x5B) || Down(0x5C), Native.ClassOf(window), Native.ClassOf(GetAncestor(info.Focus, 1)), Native.ClassOf(info.Focus), info.Caret != IntPtr.Zero);
    }
    public void Dispose()
    {
        stop.Set();
        int id = Volatile.Read(ref thread);
        if (id != 0)
            _ = PostThreadMessage((uint)id, Quit, IntPtr.Zero, IntPtr.Zero);
    }
    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window, Data, Extra;
        public uint Id, Time;
        public int X, Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size, Flags;
        public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public Native.Rect CaretBox;
    }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern int GetMessage(out Message message, IntPtr window, uint first, uint last);
    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out Message message, IntPtr window, uint first, uint last, uint remove);
    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint thread, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
}

/// <summary>The files and folders selected in the active File Explorer tab, or on the desktop, read through the Windows Shell.</summary>
internal static class ExplorerSelection
{
    private static readonly Guid TopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837"), ShellBrowser = new("000214E2-0000-0000-C000-000000000046");
    public static string[] Read(IntPtr window)
    {
        if (Type.GetTypeFromProgID("Shell.Application") is not Type type)
            return [];
        object? shell = null;
        try
        {
            shell = Activator.CreateInstance(type);
            dynamic windows = ((dynamic)shell!).Windows();
            dynamic? view = null;
            if (Native.ClassOf(window) is "Progman" or "WorkerW")
            {
                int desktop = 0;
                view = windows.FindWindowSW(0, null, 8, ref desktop, 1);
            }
            else
            {
                // Windows 11 tabs share one window; the active tab's ShellTabWindowClass is its first.
                IntPtr tab = FindWindowEx(window, IntPtr.Zero, "ShellTabWindowClass", null);
                dynamic? first = null;
                foreach (dynamic item in windows)
                {
                    if (Convert.ToInt64(item.HWND) != window.ToInt64())
                        continue;
                    first ??= item;
                    if (tab != IntPtr.Zero && TabOf(item) == tab)
                    {
                        view = item;
                        break;
                    }
                }
                view ??= first;
            }
            if (view == null)
                return [];
            List<string> paths = [];
            foreach (dynamic selected in view.Document.SelectedItems())
            {
                // Only real files and folders: not This PC, the Recycle Bin, or files inside a zip.
                string path = selected.Path;
                if (Path.IsPathFullyQualified(path) && (File.Exists(path) || Directory.Exists(path)))
                    paths.Add(path);
            }
            return [.. paths];
        }
        catch (Exception error) when (error is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException or UnauthorizedAccessException) { return []; }
        finally
        {
            if (shell != null)
                Marshal.FinalReleaseComObject(shell);
        }
    }
    private static IntPtr TabOf(object item)
    {
        try
        {
            Guid service = TopLevelBrowser, kind = ShellBrowser;
            if (item is IServiceProvider provider && provider.QueryService(ref service, ref kind, out object browser) == 0 && browser is IOleWindow owner && owner.GetWindow(out IntPtr tab) == 0)
                return tab;
        }
        catch (Exception error) when (error is COMException or InvalidCastException) { }
        return IntPtr.Zero;
    }
    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        [PreserveSig] int QueryService(ref Guid service, ref Guid kind, [MarshalAs(UnmanagedType.IUnknown)] out object value);
    }
    // IShellBrowser starts with IOleWindow.GetWindow, which is all that is needed here.
    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IOleWindow
    {
        [PreserveSig] int GetWindow(out IntPtr window);
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? className, string? title);
}

/// <summary>The device ring: a round window at the mouse pointer with the connected PCs and phones around it. The receiver still approves every send.</summary>
internal sealed class SendRing
{
    private const double Size = 420, Middle = Size / 2, Orbit = 140;
    private static Window? open;
    private readonly AppShell app;
    private readonly string[] paths;
    private readonly List<(string Name, Peer? Peer, BrowserDevice? Phone)> targets = [];
    private readonly List<Button> buttons = [];
    private readonly Window window;
    private bool closing;
    private SendRing(AppShell app, string[] paths)
    {
        this.app = app;
        this.paths = paths;
        foreach (Peer peer in app.Engine.Peers.Where(p => p.Online && p.Paired && !p.Blocked).OrderBy(p => p.Name).Take(8))
            targets.Add((peer.Name, peer, null));
        foreach (BrowserDevice phone in app.Engine.Browser.Browsers.Where(b => b.Approved).Take(8 - targets.Count))
            targets.Add((phone.Label, null, phone));
        window = new()
        {
            Title = "Send with hopo",
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true,
            Width = Size,
            Height = Size,
            Opacity = 0,
            FontFamily = app.Window.FontFamily
        };
        // The ring uses the main window's colours and styles, so it follows Light, Dark and contrast themes.
        foreach (object key in app.Window.Resources.Keys)
            window.Resources[key] = app.Window.Resources[key];
        window.Content = Build();
        window.KeyDown += Pressed;
        window.Deactivated += (_, _) => Close();
        window.Closed += (_, _) => { if (open == window) open = null; };
    }
    /// <summary>The ring's content for the preview screenshot checks, without showing it or reading Explorer.</summary>
    public static FrameworkElement Preview(AppShell app, string[] paths) => (FrameworkElement)new SendRing(app, paths).window.Content;
    /// <summary>Opens the ring for the selection in <paramref name="explorer"/> (an Explorer window or the desktop). Nothing selected: nothing opens.</summary>
    public static void Open(AppShell app, IntPtr explorer)
    {
        if (open != null)
            return;
        string[] paths = ExplorerSelection.Read(explorer);
        if (paths.Length == 0)
        {
            app.Tell("Select files or folders in File Explorer, then press " + app.Store.Preferences.SendShortcut + ".");
            return;
        }
        SendRing ring = new(app, paths);
        open = ring.window;
        ring.Show();
    }
    private Canvas Build()
    {
        Canvas stage = new()
        {
            Width = Size,
            Height = Size
        };
        Ellipse plate = new()
        {
            Width = Size - 8,
            Height = Size - 8,
            StrokeThickness = 1
        };
        plate.SetResourceReference(Shape.FillProperty, "Menu");
        plate.SetResourceReference(Shape.StrokeProperty, "GlassEdge");
        Place(stage, plate, 4, 4);
        Ellipse orbit = new()
        {
            Width = Orbit * 2,
            Height = Orbit * 2,
            StrokeThickness = 1,
            Opacity = 0.8,
            IsHitTestVisible = false
        };
        orbit.SetResourceReference(Shape.StrokeProperty, "GlassBorder");
        Place(stage, orbit, Middle - Orbit, Middle - Orbit);
        int files = paths.Count(File.Exists), folders = paths.Length - files;
        long bytes = paths.Where(File.Exists).Sum(path => new FileInfo(path).Length);
        string what = string.Join(", ", new[] { files > 0 ? files + (files == 1 ? " file" : " files") : "", folders > 0 ? folders + (folders == 1 ? " folder" : " folders") : "" }.Where(part => part.Length > 0));
        TextBlock title = C.Text("Send " + what, 15, bold: true), detail = C.Text(files > 0 ? Amount(bytes) + (folders > 0 ? " plus folder contents" : "") : "Folder contents are counted when sent", 11, "Muted"), hint = C.Text(targets.Count == 0 ? "No PCs or phones are connected right now." : "Click a device or press its number. Esc closes.", 11, "Muted");
        foreach (TextBlock text in new[] { title, detail, hint })
        {
            text.TextAlignment = TextAlignment.Center;
            text.MaxWidth = 190;
        }
        StackPanel center = C.Stack(C.Space(title, 4), C.Space(detail, 10), hint);
        center.Width = 200;
        Place(stage, center, Middle - 100, Middle - 46);
        for (int i = 0; i < targets.Count; i++)
        {
            double angle = (-90 + i * 360.0 / targets.Count) * Math.PI / 180;
            Button device = Device(i);
            // The device circle sits on the orbit line; its name hangs below it.
            Place(stage, device, Middle + Orbit * Math.Cos(angle) - 42, Middle + Orbit * Math.Sin(angle) - 28);
            buttons.Add(device);
        }
        return stage;
    }
    private static string Amount(long bytes) => bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.0} GB" : bytes >= 1L << 20 ? $"{bytes / (double)(1L << 20):0.0} MB" : bytes >= 1024 ? $"{bytes / 1024.0:0} KB" : bytes + " bytes";
    private static void Place(Canvas canvas, UIElement element, double left, double top)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
        canvas.Children.Add(element);
    }
    private Button Device(int index)
    {
        (string name, Peer? peer, _) = targets[index];
        Ellipse disc = new()
        {
            Width = 52,
            Height = 52,
            StrokeThickness = 1
        };
        disc.SetResourceReference(Shape.FillProperty, "Glass");
        disc.SetResourceReference(Shape.StrokeProperty, "GlassBorder");
        TextBlock letter = C.Text(name.Length > 0 ? char.ToUpperInvariant(name[0]).ToString() : "?", 20, peer != null ? "Mint" : "Phone", true);
        letter.HorizontalAlignment = HorizontalAlignment.Center;
        letter.VerticalAlignment = VerticalAlignment.Center;
        Border number = new()
        {
            Width = 18,
            Height = 18,
            CornerRadius = new(9),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = C.Text((index + 1).ToString(), 10, "OnAccent", true)
        };
        ((TextBlock)number.Child).HorizontalAlignment = HorizontalAlignment.Center;
        number.SetResourceReference(Border.BackgroundProperty, "Accent");
        Grid badge = new()
        {
            Width = 56,
            Height = 56,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        badge.Children.Add(disc);
        badge.Children.Add(letter);
        badge.Children.Add(number);
        TextBlock label = C.Text(name, 12, bold: true), kind = C.Text(peer != null ? "PC" : "Phone", 10, "Muted");
        label.TextAlignment = kind.TextAlignment = TextAlignment.Center;
        label.TextWrapping = TextWrapping.NoWrap;
        label.TextTrimming = TextTrimming.CharacterEllipsis;
        Button button = new()
        {
            Style = (Style)app.Window.FindResource("Plain"),
            Width = 84,
            Height = 92,
            Content = C.Stack(badge, C.Space(label, 0, 4), kind),
            Cursor = Cursors.Hand
        };
        AutomationProperties.SetName(button, $"{index + 1}. Send to {name}, {(peer != null ? "PC" : "phone")}");
        void Mark(bool on)
        {
            disc.StrokeThickness = on ? 2 : 1;
            disc.SetResourceReference(Shape.StrokeProperty, on ? "Accent" : "GlassBorder");
        }
        button.GotKeyboardFocus += (_, _) => Mark(true);
        button.LostKeyboardFocus += (_, _) => Mark(false);
        button.MouseEnter += (_, _) => Mark(true);
        button.MouseLeave += (_, _) => Mark(button.IsKeyboardFocused);
        button.Click += (_, _) => Send(index);
        return button;
    }
    private void Pressed(object sender, KeyEventArgs e)
    {
        int focused = buttons.FindIndex(button => button.IsKeyboardFocused);
        int number = e.Key is >= Key.D1 and <= Key.D9 ? e.Key - Key.D1 : e.Key is >= Key.NumPad1 and <= Key.NumPad9 ? e.Key - Key.NumPad1 : -1;
        if (e.Key == Key.Escape)
            Close();
        else if (number >= 0 && number < targets.Count)
            Send(number);
        else if (buttons.Count > 0 && e.Key is Key.Right or Key.Down or Key.Left or Key.Up)
            buttons[((focused < 0 ? 0 : focused + (e.Key is Key.Right or Key.Down ? 1 : -1)) + buttons.Count) % buttons.Count].Focus();
        else
            return;
        e.Handled = true;
    }
    private async void Send(int index)
    {
        (string name, Peer? peer, BrowserDevice? phone) = targets[index];
        Close();
        string what = paths.Length == 1 ? System.IO.Path.GetFileName(paths[0].TrimEnd('\\')) : paths.Length + " items";
        try
        {
            if (peer != null)
            {
                if (!app.Engine.Running)
                    app.ToggleReceiving();
                _ = app.Engine.Send(peer, paths);
                app.Notice("Sending to " + name, what + ". " + name + " approves before any file is sent; follow it in Transfers.");
            }
            else if (phone != null)
            {
                await Task.Run(() => app.Engine.Browser.ShareAsync(paths, default, phone.Handle));
                app.Notice("Sent to " + name, what + " appears on that phone's page under Get files from your PC.");
            }
        }
        catch (Exception error) { app.Error(error); }
    }
    private void Show()
    {
        Native.Point pointer = Native.Cursor();
        window.SourceInitialized += (_, _) => Native.MoveTo(Handle, pointer.X, pointer.Y, false);
        // Once the window has the pointer's screen scale, centre it on the pointer, inside the screen, then fade it in.
        window.ContentRendered += (_, _) =>
        {
            Native.MoveTo(Handle, pointer.X, pointer.Y, true);
            Native.Foreground(Handle);
            window.Activate();
            if (buttons.Count > 0)
                buttons[0].Focus();
            window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(Motion.On ? 0.16 : 0)));
        };
        window.Show();
    }
    private IntPtr Handle => new WindowInteropHelper(window).Handle;
    private void Close()
    {
        if (closing)
            return;
        closing = true;
        window.Close();
    }
}

/// <summary>Window and pointer helpers for the shortcut and the ring.</summary>
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X, Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left, Top, Right, Bottom;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor, Work;
        public int Flags;
    }
    public static string ClassOf(IntPtr window)
    {
        System.Text.StringBuilder name = new(256);
        return window != IntPtr.Zero && GetClassName(window, name, name.Capacity) > 0 ? name.ToString() : "";
    }
    public static Point Cursor() => GetCursorPos(out Point point) ? point : default;
    /// <summary>Moves the window's top-left to the point, or with <paramref name="centre"/> its middle, kept inside that screen's work area.</summary>
    public static void MoveTo(IntPtr window, int x, int y, bool centre)
    {
        if (!GetWindowRect(window, out Rect box))
            return;
        int width = box.Right - box.Left, height = box.Bottom - box.Top;
        if (centre)
        {
            MonitorInfo screen = new()
            {
                Size = Marshal.SizeOf<MonitorInfo>()
            };
            (x, y) = (x - width / 2, y - height / 2);
            if (GetMonitorInfo(MonitorFromPoint(new Point { X = x + width / 2, Y = y + height / 2 }, 2), ref screen))
                (x, y) = (Math.Clamp(x, screen.Work.Left, Math.Max(screen.Work.Left, screen.Work.Right - width)), Math.Clamp(y, screen.Work.Top, Math.Max(screen.Work.Top, screen.Work.Bottom - height)));
        }
        _ = SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, 0x0001 | 0x0004 | 0x0010);
    }
    /// <summary>Brings the ring to the front from the tray: Windows allows it when it briefly shares input with the window in front.</summary>
    public static void Foreground(IntPtr window)
    {
        uint front = GetWindowThreadProcessId(GetForegroundWindow(), out _), self = GetCurrentThreadId();
        bool joined = front != 0 && front != self && AttachThreadInput(self, front, true);
        _ = SetForegroundWindow(window);
        if (joined)
            _ = AttachThreadInput(self, front, false);
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int capacity);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Rect box);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attach, uint to, bool join);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);
}
