using Lantoss.Core;

namespace Lantoss.Networking;

public sealed record BrowserFile(string Id, string Name, long Size, string Path, DateTime ModifiedUtc, string Recipient = "");

/// <summary>A phone or browser connected to the phone page; on Android there are never any.</summary>
public sealed record BrowserDevice(string Handle, string Name, string Device, string Address, DateTime ConnectedUtc, DateTime LastSeenUtc, byte[]? Picture, bool Approved = true, bool Trusted = false)
{
    public string Label => Name.Length > 0 ? Name + " (" + Device + ")" : Device;
}

/// <summary>
/// Android stand-in for the phone page server. ASP.NET Core does not run on Android, and a phone does not host the page for other phones,
/// so this keeps only the members the transfer engine uses: shared notes and the shared-folder catalog. Nothing ever listens.
/// </summary>
public sealed class BrowserPortal : IDisposable
{
    public const int NotesPerWindow = 20;
    public BrowserPortal(MetadataStore store, TransferRegistry transfers, PairingCode code, bool allowLoopback = false, string? deviceKey = null)
    {
        Notes = new(store.Root);
        Spaces = new(store);
    }
    public event Action? Changed
    {
        add
        {
        }
        remove
        {
        }
    }
    public SharedNotes Notes
    {
        get;
    }
    public SpaceCatalog Spaces
    {
        get;
    }
    public bool Running => false;
    public bool Https => false;
    public string Url => "";
    public string DeviceUrl => "";
    public Task WhenStopped => Task.CompletedTask;
    public Func<ReceiveRequest, CancellationToken, Task<ReceiveDecision>>? ApproveReceive
    {
        get; set;
    }
    public Func<object[]>? AvailableBrowserDevices
    {
        get; set;
    }
    public BrowserDevice[] Devices => [];
    public BrowserFile[] Files => [];
    public string[] BlockedAddresses => [];
    public string IpUrl => "";
    public string LocalAddressStatus => "The phone page runs on computers, not on a phone.";
    public string CertificateFingerprint => "";
    public void ResetConnections()
    {
    }
    public void ClearFiles()
    {
    }
    public void Remove(string id)
    {
    }
    public void Approve(string handle)
    {
    }
    public void Block(string handle)
    {
    }
    public void Unblock(string address)
    {
    }
    public void Disconnect(string handle)
    {
    }
    public BrowserDevice[] Browsers => [];
    public event Action<BrowserDevice>? ApprovalRequested
    {
        add
        {
        }
        remove
        {
        }
    }
    public Task ShareAsync(IEnumerable<string> paths, CancellationToken token = default, string recipient = "") => Task.CompletedTask;
    public Task StartAsync(NetworkAdapter network, int port = 80) => Task.CompletedTask;
    public void ApplySecurityPolicy()
    {
    }
    public void Stop()
    {
    }
    public void Dispose()
    {
    }
    public static string PublicError(Exception error) => TransferEngine.HumanError(error);
}
