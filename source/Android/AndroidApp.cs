using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Lantoss.Android;

/// <summary>The Android process: starts the shared engine before the first screen, and provides notifications and the Downloads folder.</summary>
[Application(Label = "hopo", Icon = "@drawable/icon", AllowBackup = false)]
public sealed class AndroidApp : AvaloniaAndroidApplication<App>, IPhonePlatform
{
    private const string Channel = "hopo";
    private WifiManager.MulticastLock? multicast;
    public AndroidApp(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }
    public override void OnCreate()
    {
        // Anything that would close the app is written down first, so the next launch can show what happened.
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
            MainView.WireEngine();
        }
        catch (Exception error)
        {
            HopoHost.StartupError = error.GetType().Name + ": " + error.Message;
            CrashReport.Save(error, "Start");
        }
        MainView.Platform = this;
        // Android drops multicast packets unless an app holds this lock, and PCs are found through multicast.
        if (GetSystemService(WifiService) is WifiManager wifi)
        {
            multicast = wifi.CreateMulticastLock("hopo discovery");
            multicast.SetReferenceCounted(false);
            multicast.Acquire();
        }
        if (GetSystemService(NotificationService) is NotificationManager manager)
            manager.CreateNotificationChannel(new NotificationChannel(Channel, "Transfers", NotificationImportance.High) { Description = "Incoming files and finished transfers" });
        return base.CustomizeAppBuilder(builder);
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
