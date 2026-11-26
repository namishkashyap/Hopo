using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Lantoss.Android;

/// <summary>The Android process: opens the data store before the first screen, and provides notifications and the Downloads folder.</summary>
[Application(Label = "hopo", Icon = "@drawable/icon", AllowBackup = false)]
public sealed class AndroidApp : AvaloniaAndroidApplication<App>
{
    public static AndroidApp? Current
    {
        get; private set;
    }
    private const string Channel = "hopo", Quiet = "hopo-quiet";
    private WifiManager.MulticastLock? multicast;
    private PowerManager.WakeLock? awake;
    private WifiManager.WifiLock? fastWifi;
    private static readonly Dictionary<int, Action<int>> replies = [];
    private static int nextRequest;
    /// <summary>
    /// An incoming transfer as a notification with Decline, Accept and (for a paired PC) Always accept, which work without opening hopo.
    /// Returns what removes the notification once the transfer is answered in the app or here.
    /// </summary>
    public Action Incoming(Lantoss.Core.ReceiveRequest request, Action<int> answer)
    {
        int id;
        lock (replies)
        {
            id = ++nextRequest;
            replies[id] = answer;
        }
        void Dismiss()
        {
            lock (replies)
                replies.Remove(id);
            (GetSystemService(NotificationService) as NotificationManager)?.Cancel(Tag, id);
        }
        if (GetSystemService(NotificationService) is not NotificationManager manager)
            return Dismiss;
        PendingIntent Reply(int choice)
        {
            Intent intent = new(this, typeof(ReplyReceiver));
            intent.PutExtra(ReplyReceiver.Request, id);
            intent.PutExtra(ReplyReceiver.Choice, choice);
            return PendingIntent.GetBroadcast(this, id * 4 + choice, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
        }
        Intent open = new(this, typeof(MainActivity));
        open.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        string text = $"{request.Manifest.FileCount} {(request.Manifest.FileCount == 1 ? "file" : "files")} · {Lantoss.Core.Format.Bytes(request.Manifest.TotalBytes)}";
        // With hopo on screen its own panel already asks; the notification then waits quietly in the shade instead of covering the panel.
        Notification.Builder builder = new Notification.Builder(this, MainActivity.OnScreen ? Quiet : Channel)
            .SetContentTitle(request.PeerName + " wants to send you files")
            .SetContentText(text)
            .SetSmallIcon(Resource.Drawable.icon)
            .SetCategory(Notification.CategoryMessage)
            .SetOngoing(true)
            .SetContentIntent(PendingIntent.GetActivity(this, id * 4 + 3, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent))
            .AddAction(new Notification.Action.Builder(null, "Decline", Reply(0)).Build())
            .AddAction(new Notification.Action.Builder(null, "Accept", Reply(1)).Build());
        if (!request.Browser && request.Fingerprint.Length > 0)
            builder.AddAction(new Notification.Action.Builder(null, "Always accept", Reply(2)).Build());
        manager.Notify(Tag, id, builder.Build()!);
        return Dismiss;
    }
    private const string Tag = "hopo-incoming";
    /// <summary>A button on an incoming-files notification was tapped.</summary>
    public static void Answer(int id, int choice)
    {
        Action<int>? answer;
        lock (replies)
            replies.Remove(id, out answer);
        (Current?.GetSystemService(NotificationService) as NotificationManager)?.Cancel(Tag, id);
        answer?.Invoke(choice);
    }
    /// <summary>
    /// While files move, the processor and Wi-Fi stay awake: otherwise Android slows both when the screen is off or hopo is in the
    /// background, and transfers crawl. Released as soon as no transfer is running.
    /// </summary>
    public void KeepAwake(bool on)
    {
        lock (replies)
        {
            if (on)
            {
                if (awake == null && GetSystemService(PowerService) is PowerManager power)
                {
                    awake = power.NewWakeLock(WakeLockFlags.Partial, "hopo:transfer");
                    awake!.SetReferenceCounted(false);
                    awake.Acquire(TimeSpan.FromHours(2).Ticks / TimeSpan.TicksPerMillisecond);
                }
                if (fastWifi == null && GetSystemService(WifiService) is WifiManager wifi)
                {
                    fastWifi = wifi.CreateWifiLock(Build.VERSION.SdkInt >= BuildVersionCodes.Q ? global::Android.Net.WifiMode.FullLowLatency : global::Android.Net.WifiMode.FullHighPerf, "hopo transfer");
                    fastWifi!.SetReferenceCounted(false);
                    fastWifi.Acquire();
                }
                return;
            }
            if (awake is { IsHeld: true })
                awake.Release();
            awake = null;
            if (fastWifi is { IsHeld: true })
                fastWifi.Release();
            fastWifi = null;
        }
    }
    public AndroidApp(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }
    public override void OnCreate()
    {
        // Anything that would close the app is written down first, so the next launch can show what happened.
        Current = this;
        CrashReport.Initialize(FilesDir!.AbsolutePath);
        AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => CrashReport.Save(e.Exception, "Android");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashReport.Save(e.ExceptionObject as Exception, "App");
        TaskScheduler.UnobservedTaskException += (_, e) => { CrashReport.Save(e.Exception, "Background task"); e.SetObserved(); };
        base.OnCreate();
    }
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        string maker = Build.Manufacturer ?? "", model = Build.Model ?? "Android phone";
        string name = maker.Length > 0 && !model.StartsWith(maker, StringComparison.OrdinalIgnoreCase) ? char.ToUpperInvariant(maker[0]) + maker[1..] + " " + model : model;
        // A failure here is shown on screen with its reason, instead of the app closing.
        try
        {
            // The real paths: /data/user/0 is a link to /data/data, and hopo refuses folders reached through links.
            HopoHost.Initialize(new Java.IO.File(FilesDir!.AbsolutePath).CanonicalPath, new Java.IO.File(CacheDir!.AbsolutePath).CanonicalPath, name);
        }
        catch (Exception error)
        {
            HopoHost.StartupError = error.GetType().Name + ": " + error.Message;
            CrashReport.Save(error, "Start");
        }
        // Android drops multicast packets unless an app holds this lock, and PCs are found through multicast.
        if (GetSystemService(WifiService) is WifiManager wifi)
        {
            multicast = wifi.CreateMulticastLock("hopo discovery");
            multicast.SetReferenceCounted(false);
            multicast.Acquire();
        }
        if (GetSystemService(NotificationService) is NotificationManager manager)
        {
            manager.CreateNotificationChannel(new NotificationChannel(Channel, "Transfers", NotificationImportance.High) { Description = "Incoming files and finished transfers" });
            manager.CreateNotificationChannel(new NotificationChannel(Quiet, "Transfers while hopo is open", NotificationImportance.Low) { Description = "Incoming files while hopo is on screen; hopo asks in the app itself" });
        }
        // The same Inter typeface as hopo on the PC, Mac and Linux.
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
    public void Notify(string title, string text)
    {
        if (GetSystemService(NotificationService) is not NotificationManager manager)
            return;
        Intent open = new(this, typeof(MainActivity));
        open.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
        PendingIntent? tap = PendingIntent.GetActivity(this, 0, open, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        Notification notification = new Notification.Builder(this, Channel).SetContentTitle(title).SetContentText(text).SetSmallIcon(Resource.Drawable.icon).SetContentIntent(tap).SetAutoCancel(true).Build()!;
        manager.Notify(Math.Abs(title.GetHashCode()), notification);
    }
    /// <summary>Received files move from the private inbox to Downloads/hopo, keeping sub-folders, so Files and the gallery see them.</summary>
    public int SaveToDownloads(string folder)
    {
        int saved = 0;
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(folder, Path.GetDirectoryName(file)!).Replace('\\', '/');
            ContentValues values = new();
            values.Put(MediaStore.IMediaColumns.DisplayName, Path.GetFileName(file));
            values.Put(MediaStore.IMediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads + "/hopo" + (relative == "." ? "" : "/" + relative));
            values.Put(MediaStore.IMediaColumns.IsPending, 1);
            try
            {
                global::Android.Net.Uri? item = ContentResolver!.Insert(MediaStore.Downloads.ExternalContentUri!, values);
                if (item == null)
                    continue;
                using (Stream? target = ContentResolver.OpenOutputStream(item))
                using (FileStream source = File.OpenRead(file))
                    source.CopyTo(target!);
                ContentValues done = new();
                done.Put(MediaStore.IMediaColumns.IsPending, 0);
                ContentResolver.Update(item, done, null, null);
                File.Delete(file);
                saved++;
            }
            catch (Exception error) when (error is IOException or Java.Lang.Exception or UnauthorizedAccessException) { }
        }
        if (saved > 0 && !Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Any())
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException) { }
        return saved;
    }
}
