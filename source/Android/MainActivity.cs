using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Avalonia.Android;
using Avalonia.Threading;

namespace Lantoss.Android;

[Activity(Label = "hopo", Theme = "@style/HopoTheme", Icon = "@drawable/icon", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
[IntentFilter([Intent.ActionSend, Intent.ActionSendMultiple], Categories = [Intent.CategoryDefault], DataMimeType = "*/*")]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu && CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            RequestPermissions([global::Android.Manifest.Permission.PostNotifications], 1);
        HandleShare(Intent);
    }
    /// <summary>"Keep receiving in the background when I leave hopo" (on unless turned off in Settings).</summary>
    private static bool Background => !HopoHost.Ready || HopoHost.Store.Preferences.CloseToTray;
    /// <summary>hopo is the app on screen (between OnResume and OnPause).</summary>
    public static bool OnScreen
    {
        get; private set;
    }
    protected override void OnResume()
    {
        base.OnResume();
        OnScreen = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Lantoss.Desktop.Motion.Paused = false);
    }
    protected override void OnPause()
    {
        OnScreen = false;
        // Nothing is drawn while hopo is in the background: the rings and glows stop until it comes back.
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Lantoss.Desktop.Motion.Paused = true);
        base.OnPause();
    }
    protected override void OnStart()
    {
        base.OnStart();
        if (!Background)
            return;
        try
        {
            ReceiveService.Start(this);
        }
        catch (Exception error) { CrashReport.Save(error, "Background service"); }
    }
    protected override void OnStop()
    {
        base.OnStop();
        // With background receiving turned off, the "hopo is ready" service ends when hopo leaves the screen.
        if (!Background)
            StopService(new Intent(this, typeof(ReceiveService)));
    }
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleShare(intent);
    }
    /// <summary>"Share to hopo" from another app: shared files open Send with them selected; shared text or a link opens Text &amp; links.</summary>
    private void HandleShare(Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionSend or Intent.ActionSendMultiple))
            return;
        List<global::Android.Net.Uri> uris = [];
        if (intent.Action == Intent.ActionSendMultiple && Streams(intent) is { } many)
            uris.AddRange(many);
        else if (Stream(intent) is { } one)
            uris.Add(one);
        string? text = intent.GetStringExtra(Intent.ExtraText);
        // Handled once: rotating the screen must not share the same items again.
        intent.SetAction("");
        if (uris.Count == 0 && string.IsNullOrWhiteSpace(text))
            return;
        global::Android.Content.ContentResolver resolver = ContentResolver!;
        // The copies are made on a worker thread: a large shared video used to freeze the app (and could close it) while it was copied.
        _ = Task.Run(async () =>
        {
            string folder = HopoHost.NewBatch();
            List<string> paths = [];
            string? failure = null;
            List<global::Android.Net.Uri> chosen = [.. uris.Take(256)];
            for (int n = 0; n < chosen.Count; n++)
                try
                {
                    string name = HopoHost.SafeName(DisplayName(chosen[n]) ?? "shared file");
                    Say(chosen.Count == 1 ? "Preparing " + name + "…" : $"Preparing file {n + 1} of {chosen.Count}: {name}…");
                    string path = Path.Combine(folder, name);
                    for (int i = 2; File.Exists(path); i++)
                        path = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}");
                    using Stream? source = resolver.OpenInputStream(chosen[n]);
                    if (source == null)
                        continue;
                    using FileStream target = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
                    source.CopyTo(target, 1 << 20);
                    paths.Add(path);
                }
                catch (Exception error) when (error is IOException or Java.Lang.Exception or UnauthorizedAccessException) { failure = error.Message; }
            if (paths.Count == 0 && string.IsNullOrWhiteSpace(text))
            {
                Say("The shared files could not be read" + (failure is { Length: > 0 } ? ": " + failure : "."));
                return;
            }
            Lantoss.Desktop.Shell? shell = await Screen();
            if (shell == null)
                return;
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (paths.Count > 0)
                    await shell.AddSelectionsAsync(paths);
                else
                {
                    shell.PendingNote = text;
                    shell.Navigate("Text & links");
                }
            });
        });
    }
    /// <summary>The shell once it is on screen (it may still be starting when a share arrives).</summary>
    private static async Task<Lantoss.Desktop.Shell?> Screen()
    {
        for (int i = 0; i < 50 && Lantoss.Desktop.Shell.Current == null; i++)
            await Task.Delay(100);
        return Lantoss.Desktop.Shell.Current;
    }
    private static void Say(string message) => Dispatcher.UIThread.Post(() => Lantoss.Desktop.Shell.Current?.Tell(message));
    private static global::Android.Net.Uri? Stream(Intent intent) => Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? (global::Android.Net.Uri?)intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) : (global::Android.Net.Uri?)intent.GetParcelableExtra(Intent.ExtraStream);
    private static IEnumerable<global::Android.Net.Uri>? Streams(Intent intent) => (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? intent.GetParcelableArrayListExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) : intent.GetParcelableArrayListExtra(Intent.ExtraStream))?.OfType<global::Android.Net.Uri>();
    private string? DisplayName(global::Android.Net.Uri uri)
    {
        using global::Android.Database.ICursor? cursor = ContentResolver!.Query(uri, [IOpenableColumns.DisplayName], null, null, null);
        return cursor != null && cursor.MoveToFirst() ? cursor.GetString(0) : uri.LastPathSegment;
    }
}
