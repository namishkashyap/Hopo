using Avalonia.Platform.Storage;
using Lantoss.Core;

namespace Lantoss.Android;

/// <summary>The phone's data store and device key, opened once per app process before the first screen. The shared hopo shell (source/Desktop) runs on top of them.</summary>
internal static class HopoHost
{
    private static readonly object gate = new();
    public static MetadataStore Store { get; private set; } = null!;
    public static Identity Identity { get; private set; } = null!;
    /// <summary>Private receive area; each incoming transfer gets its own folder and is moved to Downloads/hopo once finished.</summary>
    public static string Inbox { get; private set; } = "";
    /// <summary>Picked and shared files are copied here, sent, then deleted.</summary>
    public static string Outgoing { get; private set; } = "";
    public static string? StartupError
    {
        get; set;
    }
    public static bool Ready
    {
        get; private set;
    }
    public static void Initialize(string filesDirectory, string cacheDirectory, string deviceName)
    {
        lock (gate)
        {
            if (Ready)
                return;
            string data = Path.Combine(filesDirectory, "Lantoss");
            Directory.CreateDirectory(data);
            MetadataStore store = new(data);
            Inbox = Path.Combine(filesDirectory, "Inbox");
            Outgoing = Path.Combine(cacheDirectory, "outgoing");
            Directory.CreateDirectory(Inbox);
            Directory.CreateDirectory(Outgoing);
            // Copies of picked and shared files are kept a day, for retries, then removed.
            foreach (string batch in Directory.EnumerateDirectories(Outgoing).Where(folder => Directory.GetCreationTimeUtc(folder) < DateTime.UtcNow.AddDays(-1)))
                try
                {
                    Directory.Delete(batch, true);
                }
                catch (IOException) { }
            // First run: name the phone after its model and receive into the private inbox. A phone never hosts the phone page.
            if (!string.Equals(store.Preferences.ReceiveFolder, Inbox, StringComparison.Ordinal))
                store.SetPreferences(store.Preferences with
                {
                    DeviceName = CleanName(deviceName),
                    ReceiveFolder = Inbox,
                    BrowserSharingEnabled = false,
                    SenderFolders = false
                });
            Identity = Identity.Portable(store);
            Store = store;
            Ready = true;
        }
    }
    /// <summary>A file name that is safe on every PC: no folders, control characters or reserved names.</summary>
    public static string SafeName(string name)
    {
        string clean = new string(Path.GetFileName(name.Replace('\\', '/').Split('/').Last()).Where(c => !char.IsControl(c) && !FileCatalog.Hidden(c) && c is not ('<' or '>' or ':' or '"' or '|' or '?' or '*')).ToArray()).Trim().TrimEnd('.');
        return clean.Length == 0 ? "shared file" : clean.Length > 120 ? clean[..120] : clean;
    }
    /// <summary>A new folder for one picked or shared batch, so names never collide between batches.</summary>
    public static string NewBatch()
    {
        string folder = Path.Combine(Outgoing, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
    /// <summary>Copies a folder chosen in Android's folder picker, with its subfolders, into the app's cache so it is sent as one folder. Files that can't be read are left out and counted.</summary>
    public static async Task<string?> ImportFolderAsync(IStorageFolder folder, Action<string> progress)
    {
        string root = Path.Combine(NewBatch(), SafeName(folder.Name));
        int copied = 0, failed = 0;
        async Task CopyAsync(IStorageFolder source, string target, int depth)
        {
            Directory.CreateDirectory(target);
            if (depth > 32)
                return;
            await foreach (IStorageItem item in source.GetItemsAsync())
            {
                if (item is IStorageFolder child)
                    await CopyAsync(child, Path.Combine(target, SafeName(child.Name)), depth + 1);
                else if (item is IStorageFile file)
                {
                    string path = Path.Combine(target, SafeName(file.Name));
                    bool created = false;
                    try
                    {
                        progress($"Preparing {folder.Name}: file {copied + failed + 1}, {file.Name}…");
                        await using Stream input = await file.OpenReadAsync();
                        await Task.Run(async () =>
                        {
                            await using FileStream output = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20, FileOptions.Asynchronous | FileOptions.SequentialScan);
                            created = true;
                            await input.CopyToAsync(output, 1 << 20);
                        });
                        copied++;
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException or Java.Lang.Exception) { failed++; if (created) try { File.Delete(path); } catch (IOException) { } }
                }
            }
        }
        try
        {
            await CopyAsync(folder, root, 0);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Java.Lang.Exception) { return null; }
        progress(failed > 0 ? $"{failed} of {copied + failed} files in {folder.Name} could not be read and were left out." : $"{folder.Name} is ready to send ({copied} files).");
        return root;
    }
    public static async Task<string?> ImportAsync(IStorageFile file)
    {
        try
        {
            string folder = NewBatch(), path = Path.Combine(folder, SafeName(file.Name));
            await using Stream source = await file.OpenReadAsync();
            // Copied on a worker thread with a 1 MB buffer: Android's document streams read synchronously, which froze the screen for large videos.
            await Task.Run(async () =>
            {
                await using FileStream target = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await source.CopyToAsync(target, 1 << 20);
            });
            return path;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Java.Lang.Exception) { return null; }
    }
    private static string CleanName(string name)
    {
        string clean = new string(name.Where(c => !FileCatalog.Hidden(c)).ToArray()).Trim();
        return clean.Length is > 0 and <= 64 ? clean : clean.Length > 64 ? clean[..64].Trim() : "Android phone";
    }
}
