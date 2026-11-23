using Lantoss.Core;
using Lantoss.Networking;

namespace Lantoss.Android;

/// <summary>One store and transfer engine for the whole app process. The activity can be recreated (rotation, returning from the share sheet) without stopping transfers.</summary>
internal static class HopoHost
{
    private static readonly object gate = new();
    public static MetadataStore Store { get; private set; } = null!;
    public static TransferEngine Engine { get; private set; } = null!;
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
    public static event Action? Started;
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
            // First run: name the phone after its model and receive into the private inbox. A phone never hosts the phone page.
            if (!string.Equals(store.Preferences.ReceiveFolder, Inbox, StringComparison.Ordinal))
                store.SetPreferences(store.Preferences with
                {
                    DeviceName = CleanName(deviceName),
                    ReceiveFolder = Inbox,
                    BrowserSharingEnabled = false,
                    SenderFolders = false
                });
            Identity identity = Identity.Portable(store);
            Store = store;
            Engine = new(store, identity);
            Ready = true;
        }
    }
    /// <summary>Starts receiving on the first private Wi-Fi network; returns a message when that is not possible yet.</summary>
    public static string? StartReceiving()
    {
        if (Engine.Running)
            return null;
        NetworkAdapter? network = NetworkAdapter.Available().FirstOrDefault();
        if (network == null)
            return "Connect this phone to your home Wi-Fi to find your PCs.";
        try
        {
            Engine.Start(network);
            Started?.Invoke();
            return null;
        }
        catch (Exception error)
        {
            CrashReport.Save(error, "Start receiving");
            return "hopo could not start receiving. " + TransferEngine.HumanError(error);
        }
    }
    private static string CleanName(string name)
    {
        string clean = new string(name.Where(c => !FileCatalog.Hidden(c)).ToArray()).Trim();
        return clean.Length is > 0 and <= 64 ? clean : clean.Length > 64 ? clean[..64].Trim() : "Android phone";
    }
}
