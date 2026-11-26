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
    private readonly Action<int> ringKey;
    private readonly ManualResetEvent stop = new(false);
    private readonly HookProc callback;
    private volatile string setting;
    private int thread;
    public ShortcutWatcher(string setting, Action<IntPtr> pressed, Action<int> ringKey)
    {
        this.setting = setting;
        this.pressed = pressed;
        this.ringKey = ringKey;
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
        // While the ring is open, Escape closes it and 1–9 send, wherever the keyboard focus is.
        if (code >= 0 && SendRing.Showing && (message == KeyDown || message == SystemKeyDown) && Marshal.ReadInt32(data) is int key && (key == 0x1B || key is >= 0x31 and <= 0x39 || key is >= 0x61 and <= 0x69))
        {
            ringKey(key);
            return 1;
        }
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
    /// <summary>Reads the selection on its own single-threaded COM thread, so the window never waits for Explorer.</summary>
    public static Task<string[]> ReadAsync(IntPtr window)
    {
        TaskCompletionSource<string[]> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread reader = new(() =>
        {
            try
            {
                done.TrySetResult(Read(window));
            }
            catch (Exception error) { done.TrySetException(error); }
        })
        {
            IsBackground = true,
            Name = "hopo Explorer selection"
        };
        reader.SetApartmentState(ApartmentState.STA);
        reader.Start();
        return done.Task;
    }
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
    // A 312 px circle: Home's waves as a faint border round its edge, the devices on a quiet inner orbit, the selection in the middle.
    private const double Size = 312, Middle = Size / 2, Orbit = 98, Badge = 40;
    // The Home ring colours: each device gets its own.
    private static readonly Color[] Palette = [.. new[] { "#B57CFF", "#3FE0FF", "#3DF5A0", "#FF6AD5", "#F5B54A", "#6FB7FF", "#FF8A80", "#5CF0D0" }.Select(hex => (Color)ColorConverter.ConvertFromString(hex))];
    private static SendRing? current;
    private static bool opening;
    /// <summary>
    /// The ring is on screen. The shortcut's keyboard hook (on its own thread) then hands Escape and 1–9 to the ring, so they work even
    /// when Windows did not give the ring the keyboard focus.
    /// </summary>
    public static volatile bool Showing;
    // Endless motion in the ring, stopped when it closes.
    private readonly List<Action> loops = [];
    private readonly AppShell app;
    private readonly string[] paths;
    private readonly List<(string Name, Peer? Peer, BrowserDevice? Phone)> targets = [];
    private readonly List<Button> buttons = [];
    private readonly Window window;
    private readonly Canvas stage, cover = new();
    private bool closing;
    // Explorer or the desktop gets the focus back when the ring closes, so hopo's own window never comes forward.
    private IntPtr returnTo;
    // Home's waves round the edge of the circle (radius about 148 px), lower and thinner than on Home, on the 30 fps clock.
    private readonly WaveRings waves = new(Size, 148.0 / 284, 0.4, 0.7, 2, 0);
    private readonly TranslateTransform tick = new();
    private readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
    private SendRing(AppShell app, string[] paths, bool preview = false)
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
            FontFamily = app.Window.FontFamily
        };
        // The ring uses the main window's colours and styles, so it follows Light, Dark and contrast themes.
        foreach (object key in app.Window.Resources.Keys)
            window.Resources[key] = app.Window.Resources[key];
        stage = Build();
        if (!preview)
        {
            // The window covers the screen the ring opens on with an almost invisible layer (1/255 opaque, so it still receives clicks):
            // a click anywhere outside the circle closes the ring, whether or not Windows gave the ring the focus.
            cover.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            // Only the circle fades in; the layer itself is there (and catches clicks) from the first moment.
            stage.Opacity = 0;
            cover.Children.Add(stage);
            cover.MouseDown += (_, e) =>
            {
                if (e.OriginalSource == cover)
                    Close(true);
            };
            window.Content = cover;
        }
        else
            window.Content = stage;
        window.KeyDown += Pressed;
        window.Deactivated += (_, _) => Close(false);
        window.Closed += (_, _) =>
        {
            foreach (Action stop in loops)
                stop();
            if (current == this)
            {
                current = null;
                Showing = false;
            }
        };
    }
    /// <summary>The ring's content for the preview screenshot checks, without showing it or reading Explorer.</summary>
    public static FrameworkElement Preview(AppShell app, string[] paths) => new SendRing(app, paths, true).stage;
    /// <summary>Opens the ring for the selection in <paramref name="explorer"/> (an Explorer window or the desktop). Nothing selected: nothing opens.</summary>
    public static async void Open(AppShell app, IntPtr explorer)
    {
        if (current != null || opening)
            return;
        opening = true;
        string[] paths;
        try
        {
            // Explorer is asked on its own thread with a time limit, so a busy Explorer never freezes hopo.
            paths = await ExplorerSelection.ReadAsync(explorer).WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (TimeoutException) { paths = []; }
        finally { opening = false; }
        if (paths.Length == 0)
        {
            app.Tell("Select files or folders in File Explorer, then press " + app.Store.Preferences.SendShortcut + ".");
            return;
        }
        SendRing ring = new(app, paths)
        {
            returnTo = explorer
        };
        current = ring;
        Showing = true;
        ring.Show();
    }
    /// <summary>A key caught by the shortcut's keyboard hook while the ring is open: Escape closes it, 1–9 send to that device.</summary>
    public static void Key(int virtualKey)
    {
        if (current is not { } ring)
            return;
        int number = virtualKey is >= 0x31 and <= 0x39 ? virtualKey - 0x31 : virtualKey is >= 0x61 and <= 0x69 ? virtualKey - 0x61 : -1;
        if (virtualKey == 0x1B)
            ring.Close(true);
        else if (number >= 0 && number < ring.targets.Count)
            ring.Send(number);
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
        // A faint violet haze in the middle and a soft ripple that grows out of it every few seconds.
        Place(stage, Glow(Palette[0], 180, 0.18), Middle - 90, Middle - 90);
        ScaleTransform spread = new(0.5, 0.5);
        Ellipse ripple = new()
        {
            Width = 150,
            Height = 150,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Palette[1]),
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new(0.5, 0.5),
            RenderTransform = spread
        };
        Place(stage, ripple, Middle - 75, Middle - 75);
        // The waves are the circle's border: thin, low and faint.
        Grid border = new()
        {
            Width = Size,
            Height = Size,
            Opacity = 0.45,
            IsHitTestVisible = false
        };
        border.Children.Add(waves.Glow);
        border.Children.Add(waves.Lines);
        Place(stage, border, 0, 0);
        tick.Changed += (_, _) => waves.Draw(watch.Elapsed.TotalSeconds * 1.5, 0);
        waves.Draw(0, 0);
        // The devices' orbit: a quiet guide line.
        Ellipse orbit = new()
        {
            Width = Orbit * 2,
            Height = Orbit * 2,
            StrokeThickness = 1,
            Opacity = 0.55,
            IsHitTestVisible = false
        };
        orbit.SetResourceReference(Shape.StrokeProperty, "GlassBorder");
        Place(stage, orbit, Middle - Orbit, Middle - Orbit);
        if (Motion.On)
        {
            loops.Add(Motion.Unpaused(tick, TranslateTransform.XProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever }));
            DoubleAnimation grow = new(0.5, 1.15, TimeSpan.FromSeconds(3.2))
            {
                EasingFunction = Bezier.Ripple,
                RepeatBehavior = RepeatBehavior.Forever
            };
            loops.Add(Motion.Unpaused(spread, ScaleTransform.ScaleXProperty, grow));
            loops.Add(Motion.Unpaused(spread, ScaleTransform.ScaleYProperty, grow.Clone()));
            loops.Add(Motion.Unpaused(ripple, UIElement.OpacityProperty, new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(3.2), RepeatBehavior = RepeatBehavior.Forever, KeyFrames = { new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)), new LinearDoubleKeyFrame(0.3, KeyTime.FromPercent(0.15)), new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)) } }));
            // The ring pops open from the pointer.
            ScaleTransform pop = new(0.88, 0.88);
            stage.RenderTransformOrigin = new(0.5, 0.5);
            stage.RenderTransform = pop;
            DoubleAnimation popIn = new(0.88, 1, TimeSpan.FromSeconds(0.32))
            {
                EasingFunction = Bezier.Pop
            };
            pop.BeginAnimation(ScaleTransform.ScaleXProperty, popIn);
            pop.BeginAnimation(ScaleTransform.ScaleYProperty, popIn);
        }
        int files = paths.Count(File.Exists), folders = paths.Length - files;
        long bytes = paths.Where(File.Exists).Sum(path => new FileInfo(path).Length);
        string what = string.Join(", ", new[] { files > 0 ? Format.Count(files, "file") : "", folders > 0 ? Format.Count(folders, "folder") : "" }.Where(part => part.Length > 0));
        TextBlock title = C.Text("Send " + what, 12, bold: true), detail = C.Text(files > 0 ? Amount(bytes) + (folders > 0 ? " plus folders" : "") : "Counted when sent", 10, "Muted"), hint = C.Text(targets.Count == 0 ? "No PCs or phones connected" : targets.Count == 1 ? "Click it or press 1" : "Click or press 1–" + targets.Count, 9.5, "Muted");
        foreach (TextBlock text in new[] { title, detail, hint })
        {
            text.TextAlignment = TextAlignment.Center;
            text.MaxWidth = 84;
            text.TextWrapping = TextWrapping.Wrap;
        }
        // A glass disc in the middle, like this PC on Home.
        StackPanel center = C.Stack(C.Space(title, 2), C.Space(detail, 4), hint);
        center.VerticalAlignment = VerticalAlignment.Center;
        center.HorizontalAlignment = HorizontalAlignment.Center;
        Border self = new()
        {
            Width = 96,
            Height = 96,
            CornerRadius = new(48),
            BorderThickness = new(1),
            Child = center
        };
        self.SetResourceReference(Border.BackgroundProperty, "Glass");
        self.SetResourceReference(Border.BorderBrushProperty, "GlassEdge");
        Place(stage, self, Middle - 48, Middle - 48);
        for (int i = 0; i < targets.Count; i++)
        {
            double angle = (-90 + i * 360.0 / targets.Count) * Math.PI / 180;
            Button device = Device(i);
            // The device's icon sits on the orbit; its name label hangs just below it.
            Place(stage, device, Middle + Orbit * Math.Cos(angle) - 50, Middle + Orbit * Math.Sin(angle) - Badge / 2 - 2);
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
    /// <summary>A soft round glow: a gradient, which costs nothing to redraw, unlike a blur.</summary>
    private static Ellipse Glow(Color color, double size, double strength)
    {
        RadialGradientBrush fill = new()
        {
            GradientStops = { new(Color.FromArgb((byte)(strength * 255), color.R, color.G, color.B), 0), new(Color.FromArgb((byte)(strength * 90), color.R, color.G, color.B), 0.55), new(Color.FromArgb(0, color.R, color.G, color.B), 1) }
        };
        fill.Freeze();
        return new Ellipse { Width = size, Height = size, Fill = fill, IsHitTestVisible = false };
    }
    private static SolidColorBrush Solid(Color color, byte alpha = 255)
    {
        SolidColorBrush brush = new(Color.FromArgb(alpha, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
    private Button Device(int index)
    {
        (string name, Peer? peer, BrowserDevice? phone) = targets[index];
        Color tint = Palette[index % Palette.Length];
        // A calm icon: the phone's own picture when it has one, otherwise a small PC or phone symbol in the device's colour on a lightly
        // tinted disc with a thin edge, and a soft glow behind it.
        Ellipse halo = Glow(tint, 64, 0.4);
        halo.Opacity = 0.22;
        halo.Margin = new(-12);
        halo.RenderTransformOrigin = new(0.5, 0.5);
        halo.RenderTransform = new ScaleTransform();
        ImageSource? picture = C.Picture(phone?.Picture, (int)Badge * 2);
        RadialGradientBrush inside = new()
        {
            GradientStops = { new(Color.FromArgb(70, tint.R, tint.G, tint.B), 0), new(Color.FromArgb(28, tint.R, tint.G, tint.B), 1) }
        };
        inside.Freeze();
        Ellipse disc = new()
        {
            Width = Badge,
            Height = Badge,
            StrokeThickness = 1,
            Stroke = Solid(tint, 150),
            Fill = picture != null ? new ImageBrush(picture) { Stretch = Stretch.UniformToFill } : inside
        };
        Grid badge = new()
        {
            Width = Badge + 4,
            Height = Badge + 4,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ScaleTransform lift = new();
        badge.RenderTransformOrigin = new(0.5, 0.5);
        badge.RenderTransform = lift;
        badge.Children.Add(halo);
        badge.Children.Add(disc);
        if (picture == null)
        {
            FrameworkElement icon = C.Icon(peer != null ? "devices" : "phone", 17);
            if (icon is Viewbox { Child: System.Windows.Shapes.Path symbol })
                symbol.Stroke = Solid(tint, 230);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            badge.Children.Add(icon);
        }
        TextBlock digit = C.Text((index + 1).ToString(), 8.5, bold: true);
        digit.Foreground = Solid(Color.FromRgb(20, 22, 29));
        digit.HorizontalAlignment = HorizontalAlignment.Center;
        digit.VerticalAlignment = VerticalAlignment.Center;
        badge.Children.Add(new Border
        {
            Width = 15,
            Height = 15,
            CornerRadius = new(7.5),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Solid(tint, 235),
            Child = digit
        });
        // The name on its own dark label, so it reads clearly over the waves and the orbit.
        TextBlock label = C.Text(name, 11.5);
        label.FontWeight = FontWeights.SemiBold;
        label.TextAlignment = TextAlignment.Center;
        label.TextWrapping = TextWrapping.NoWrap;
        label.TextTrimming = TextTrimming.CharacterEllipsis;
        Border tag = new()
        {
            Child = label,
            Padding = new(7, 2, 7, 3),
            CornerRadius = new(7),
            BorderThickness = new(1),
            MaxWidth = 98,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new(0, 3, 0, 0)
        };
        tag.SetResourceReference(Border.BackgroundProperty, "Menu");
        tag.SetResourceReference(Border.BorderBrushProperty, "GlassBorder");
        Button button = new()
        {
            Style = (Style)app.Window.FindResource("Plain"),
            Width = 100,
            Height = Badge + 30,
            Content = C.Stack(badge, tag),
            Cursor = Cursors.Hand
        };
        AutomationProperties.SetName(button, $"{index + 1}. Send to {name}, {(peer != null ? "PC" : "phone")}");
        // Devices appear one after another in their places: they grow in, they never move around.
        if (Motion.On)
        {
            ScaleTransform grow = new(0.4, 0.4);
            button.RenderTransformOrigin = new(0.5, (Badge / 2 + 2) / (Badge + 30));
            button.RenderTransform = grow;
            button.Opacity = 0;
            TimeSpan begin = TimeSpan.FromSeconds(0.1 + 0.05 * index);
            DoubleAnimation appear = new(0.4, 1, TimeSpan.FromSeconds(0.45))
            {
                EasingFunction = Bezier.Spring,
                BeginTime = begin
            };
            grow.BeginAnimation(ScaleTransform.ScaleXProperty, appear);
            grow.BeginAnimation(ScaleTransform.ScaleYProperty, appear);
            button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.22)) { BeginTime = begin });
        }
        // The device under the pointer or keyboard grows a little and its glow breathes.
        Action? breathing = null;
        void Mark(bool on)
        {
            Motion.To(lift, ScaleTransform.ScaleXProperty, on ? 1.08 : 1, Motion.On ? 0.25 : 0, Bezier.Spring);
            Motion.To(lift, ScaleTransform.ScaleYProperty, on ? 1.08 : 1, Motion.On ? 0.25 : 0, Bezier.Spring);
            halo.Opacity = on ? 0.6 : 0.22;
            breathing?.Invoke();
            breathing = null;
            if (!on || !Motion.On)
                return;
            ScaleTransform pulse = (ScaleTransform)halo.RenderTransform;
            DoubleAnimation beat = new(1, 1.15, TimeSpan.FromSeconds(0.9))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Action x = Motion.Unpaused(pulse, ScaleTransform.ScaleXProperty, beat), y = Motion.Unpaused(pulse, ScaleTransform.ScaleYProperty, beat.Clone());
            breathing = () => { x(); y(); };
            loops.Add(breathing);
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
        // Escape and the number keys usually arrive through the keyboard hook (Key); these cover the ring having the focus as well.
        int focused = buttons.FindIndex(button => button.IsKeyboardFocused);
        int number = e.Key is >= System.Windows.Input.Key.D1 and <= System.Windows.Input.Key.D9 ? e.Key - System.Windows.Input.Key.D1 : e.Key is >= System.Windows.Input.Key.NumPad1 and <= System.Windows.Input.Key.NumPad9 ? e.Key - System.Windows.Input.Key.NumPad1 : -1;
        if (e.Key == System.Windows.Input.Key.Escape)
            Close(true);
        else if (number >= 0 && number < targets.Count)
            Send(number);
        else if (buttons.Count > 0 && e.Key is System.Windows.Input.Key.Right or System.Windows.Input.Key.Down or System.Windows.Input.Key.Left or System.Windows.Input.Key.Up)
            buttons[((focused < 0 ? 0 : focused + (e.Key is System.Windows.Input.Key.Right or System.Windows.Input.Key.Down ? 1 : -1)) + buttons.Count) % buttons.Count].Focus();
        else
            return;
        e.Handled = true;
    }
    private async void Send(int index)
    {
        if (closing)
            return;
        (string name, Peer? peer, BrowserDevice? phone) = targets[index];
        Close(true);
        string what = paths.Length == 1 ? System.IO.Path.GetFileName(paths[0].TrimEnd('\\')) : paths.Length + " items";
        try
        {
            if (peer != null)
            {
                if (!app.Engine.Running)
                    app.ToggleReceiving();
                _ = app.Engine.Send(peer, paths);
                app.Tell("Sending " + what + " to " + name + ". " + name + " approves before any file is sent.");
                SendToast.Show(app, "Sending to " + name, what + " · " + name + " is asked to accept", Palette[index % Palette.Length]);
            }
            else if (phone != null)
            {
                SendToast.Show(app, "Sending to " + name, what, Palette[index % Palette.Length]);
                await Task.Run(() => app.Engine.Browser.ShareAsync(paths, default, phone.Handle));
                app.Tell("Sent " + what + " to " + name + ". It appears on that phone's page under Get files from your PC.");
            }
        }
        catch (Exception error) { app.Error(error); }
    }
    private void Show()
    {
        Native.Point pointer = Native.Cursor();
        // The window takes the whole screen under the pointer (taskbar included); the circle is placed at the pointer inside it.
        window.SourceInitialized += (_, _) => Native.Cover(Handle, pointer);
        // Placed as soon as the layout is ready (not after the first frame is drawn, which could take a second on a large screen).
        window.Loaded += (_, _) =>
        {
            Point at = window.PointFromScreen(new Point(pointer.X, pointer.Y));
            Canvas.SetLeft(stage, Math.Clamp(at.X - Middle, 8, Math.Max(8, cover.ActualWidth - Size - 8)));
            Canvas.SetTop(stage, Math.Clamp(at.Y - Middle, 8, Math.Max(8, cover.ActualHeight - Size - 8)));
            Native.Foreground(Handle);
            window.Activate();
            if (buttons.Count > 0)
                buttons[0].Focus();
            stage.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(Motion.On ? 0.16 : 0)));
        };
        window.Show();
    }
    private IntPtr Handle => new WindowInteropHelper(window).Handle;
    /// <summary>Closes the ring; with <paramref name="giveBack"/> (a device chosen, Escape or a click outside) the window it was opened over gets the focus back first.</summary>
    private void Close(bool giveBack)
    {
        if (closing)
            return;
        closing = true;
        Showing = false;
        if (giveBack && returnTo != IntPtr.Zero && Native.IsWindow(returnTo))
            Native.Foreground(returnTo);
        window.Close();
    }
}

/// <summary>A small hopo notice at the bottom right after sending from the ring. It never takes the focus and fades out by itself.</summary>
internal static class SendToast
{
    private static Window? shown;
    public static void Show(AppShell app, string title, string detail, Color tint)
    {
        shown?.Close();
        TextBlock heading = C.Text(title, 13, bold: true), text = C.Text(detail, 12, "Muted");
        heading.TextTrimming = text.TextTrimming = TextTrimming.CharacterEllipsis;
        SolidColorBrush ink = new(tint);
        ink.Freeze();
        Ellipse dot = new()
        {
            Width = 10,
            Height = 10,
            Fill = ink,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(0, 0, 12, 0)
        };
        StackPanel words = C.Stack(C.Space(heading, 2), text);
        words.Width = 250;
        Border card = new()
        {
            Padding = new(16, 12, 16, 12),
            CornerRadius = new(14),
            BorderThickness = new(1),
            Child = C.Inline(dot, words)
        };
        card.SetResourceReference(Border.BackgroundProperty, "Surface");
        card.SetResourceReference(Border.BorderBrushProperty, "GlassEdge");
        Grid frame = C.Shadow(card, 14, 30, 10, 0.4);
        frame.Margin = new(20);
        Window toast = new()
        {
            Title = "hopo",
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            Opacity = 0,
            FontFamily = app.Window.FontFamily,
            Content = frame
        };
        foreach (object key in app.Window.Resources.Keys)
            toast.Resources[key] = app.Window.Resources[key];
        toast.SourceInitialized += (_, _) => Native.NoActivate(new WindowInteropHelper(toast).Handle);
        toast.MouseLeftButtonUp += (_, _) => toast.Close();
        toast.Closed += (_, _) => { if (shown == toast) shown = null; };
        toast.ContentRendered += async (_, _) =>
        {
            Rect work = SystemParameters.WorkArea;
            (toast.Left, toast.Top) = (work.Right - toast.ActualWidth, work.Bottom - toast.ActualHeight);
            toast.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(Motion.On ? 0.2 : 0)));
            await Task.Delay(3500);
            if (shown != toast)
                return;
            DoubleAnimation fade = new(0, TimeSpan.FromSeconds(Motion.On ? 0.35 : 0));
            fade.Completed += (_, _) => toast.Close();
            toast.BeginAnimation(UIElement.OpacityProperty, fade);
        };
        shown = toast;
        toast.Show();
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
    /// <summary>Sizes the window to the whole screen (monitor) under the point, taskbar included.</summary>
    public static void Cover(IntPtr window, Point point)
    {
        MonitorInfo screen = new()
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (GetMonitorInfo(MonitorFromPoint(point, 2), ref screen))
            _ = SetWindowPos(window, IntPtr.Zero, screen.Monitor.Left, screen.Monitor.Top, screen.Monitor.Right - screen.Monitor.Left, screen.Monitor.Bottom - screen.Monitor.Top, 0x0004 | 0x0010);
    }
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
    /// <summary>A window that is never activated, even when clicked, and stays out of Alt+Tab.</summary>
    public static void NoActivate(IntPtr window) => _ = SetWindowLongPtr(window, -20, GetWindowLongPtr(window, -20) | 0x08000080);
    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
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
