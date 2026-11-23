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
        try
        {
            ReceiveService.Start(this);
        }
        catch (Exception error) { CrashReport.Save(error, "Background service"); }
        HandleShare(Intent);
    }
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleShare(intent);
    }
    /// <summary>"Share to hopo" from another app: copy the shared items into the app, then ask which PC gets them.</summary>
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
        string folder = Path.Combine(HopoHost.Outgoing, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        List<string> paths = [];
        foreach (global::Android.Net.Uri uri in uris.Take(256))
            try
            {
                string name = MainView.SafeName(DisplayName(uri) ?? "shared file");
                string path = Path.Combine(folder, name);
                for (int i = 2; File.Exists(path); i++)
                    path = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}");
                using Stream? source = ContentResolver!.OpenInputStream(uri);
                if (source == null)
                    continue;
                using FileStream target = File.Create(path);
                source.CopyTo(target);
                paths.Add(path);
            }
            catch (Exception error) when (error is IOException or Java.Lang.Exception or UnauthorizedAccessException) { }
        if (paths.Count == 0 && string.IsNullOrWhiteSpace(text))
            return;
        // The screen may still be starting when the share arrives.
        Dispatcher.UIThread.Post(async () =>
        {
            for (int i = 0; i < 50 && MainView.Current == null; i++)
                await Task.Delay(100);
            if (MainView.Current is { } view)
                await view.ShareAsync([.. paths], folder, paths.Count == 0 ? text : null);
        });
    }
    private static global::Android.Net.Uri? Stream(Intent intent) => Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? (global::Android.Net.Uri?)intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) : (global::Android.Net.Uri?)intent.GetParcelableExtra(Intent.ExtraStream);
    private static IEnumerable<global::Android.Net.Uri>? Streams(Intent intent) => (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? intent.GetParcelableArrayListExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) : intent.GetParcelableArrayListExtra(Intent.ExtraStream))?.OfType<global::Android.Net.Uri>();
    private string? DisplayName(global::Android.Net.Uri uri)
    {
        using global::Android.Database.ICursor? cursor = ContentResolver!.Query(uri, [IOpenableColumns.DisplayName], null, null, null);
        return cursor != null && cursor.MoveToFirst() ? cursor.GetString(0) : uri.LastPathSegment;
    }
}
