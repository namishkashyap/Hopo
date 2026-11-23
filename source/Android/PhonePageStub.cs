using Lantoss.Core;

namespace Lantoss.Networking;

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
