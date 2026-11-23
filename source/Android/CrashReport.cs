namespace Lantoss.Android;

/// <summary>Keeps the details of a crash or failed start in the app's private folder, so the next launch can show them for copying.</summary>
internal static class CrashReport
{
    private static string? file;
    public static void Initialize(string filesDirectory) => file = Path.Combine(filesDirectory, "crash.txt");
    public static void Save(Exception? error, string where)
    {
        if (file == null || error == null)
            return;
        try
        {
            string release = global::Android.OS.Build.VERSION.Release ?? "", device = global::Android.OS.Build.Manufacturer + " " + global::Android.OS.Build.Model;
            int api = (int)global::Android.OS.Build.VERSION.SdkInt;
            string text = $"hopo {typeof(CrashReport).Assembly.GetName().Version} · Android {release} (API {api}) · {device}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss} · {where}\n{error}\n\n";
            File.AppendAllText(file, text.Length > 20000 ? text[..20000] : text);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { }
    }
    public static string? Pending()
    {
        try
        {
            return file != null && File.Exists(file) ? File.ReadAllText(file) : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
    }
    public static void Clear()
    {
        try
        {
            if (file != null)
                File.Delete(file);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
}
