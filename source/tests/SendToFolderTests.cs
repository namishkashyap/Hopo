using Lantoss.Core;
using Lantoss.Networking;

internal static partial class Program
{
    private static async Task SendToFolderCasesAsync()
    {
        MetadataStore clientStore = Store("Folder client"), hostStore = Store("Folder host");
        using Identity clientKey = Identity.Ephemeral(), hostKey = Identity.Ephemeral();
        using TransferEngine client = new(clientStore, clientKey, true, false), host = new(hostStore, hostKey, true, false);
        string documents = Path.Combine(hostStore.Root, "Documents"), pictures = Path.Combine(hostStore.Root, "Pictures"), outside = Path.Combine(hostStore.Root, "Outside");
        Directory.CreateDirectory(Path.Combine(documents, "Work", "Reports"));
        Directory.CreateDirectory(Path.Combine(documents, ".hidden"));
        Directory.CreateDirectory(pictures);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(documents, "private.txt"), "not listed");
        host.FolderRoots = () => [("Documents", documents), ("Pictures", pictures), ("Music", Path.Combine(hostStore.Root, "Missing"))];
        client.Start(Loopback, 0, false);
        host.Start(Loopback, 0, false);
        Peer hostPeer = await client.ConnectDirectAsync(new()
        {
            Fingerprint = hostKey.Fingerprint,
            Name = "Folder host",
            Address = "127.0.0.1",
            Port = host.Port,
            SupportsDirectConnect = true
        });
        string source = Path.Combine(clientStore.Root, "Recording.mp4");
        File.WriteAllText(source, "recording bytes");
        async Task<string> Fails(Func<Task> action)
        {
            try
            {
                await action();
                return "";
            }
            catch (Exception error) { return error.Message; }
        }
        string off = await Fails(() => client.BrowseFoldersAsync(hostPeer));
        Check(off.Contains("doesn't allow sending into its folders", StringComparison.Ordinal), "Send to a folder is off until the receiving PC turns it on");
        hostStore.SetPreferences(hostStore.Preferences with
        {
            FolderAccess = true
        });
        SpaceEntry[] roots = await client.BrowseFoldersAsync(hostPeer);
        Check(roots.Select(entry => entry.Name).SequenceEqual(["Documents", "Pictures"]), "The top level lists only the user folders that exist");
        SpaceEntry[] inside = await client.BrowseFoldersAsync(hostPeer, "Documents");
        Check(inside.Length == 1 && inside[0].Name == "Work" && inside[0].Path == "Documents/Work" && inside[0].Directory, "Browsing lists subfolder names only: no file names and no hidden folders");
        Check((await Fails(() => client.BrowseFoldersAsync(hostPeer, "Documents/../Outside"))).Length > 0 && (await Fails(() => client.BrowseFoldersAsync(hostPeer, "Outside"))).Length > 0, "Paths can't leave the user folders or name another folder");
        int approvals = 0;
        string? asked = null;
        host.ApproveReceive = (request, _) => { approvals++; asked = request.DefaultFolder; return Task.FromResult(new ReceiveDecision(false, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Reports", [source]);
        Check(approvals == 1 && asked == Path.Combine(documents, "Work", "Reports") && !File.Exists(Path.Combine(documents, "Work", "Reports", "Recording.mp4")), "A device that isn't trusted is asked first, with the chosen folder shown; Decline writes nothing");
        host.ApproveReceive = (request, _) => { approvals++; return Task.FromResult(new ReceiveDecision(true, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Reports", [source]);
        Check(approvals == 2 && File.ReadAllText(Path.Combine(documents, "Work", "Reports", "Recording.mp4")) == "recording bytes", "After Accept the file lands in exactly the chosen folder");
        Peer clientPeer = hostStore.Peers.Single(p => p.Fingerprint == clientKey.Fingerprint);
        host.EditPeer(clientPeer with
        {
            Trusted = true
        });
        await client.SendToFolderAsync(hostPeer, "Pictures", [source]);
        Check(approvals == 2 && File.Exists(Path.Combine(pictures, "Recording.mp4")), "A device set to Always accept sends without a prompt");
        await client.SendToFolderAsync(hostPeer, "Pictures", [source]);
        Check(File.Exists(Path.Combine(pictures, "Recording (1).mp4")), "A same-name file is kept as a copy, not overwritten");
        hostStore.SetPreferences(hostStore.Preferences with
        {
            FolderAccess = false
        });
        Check((await Fails(() => client.SendToFolderAsync(hostPeer, "Pictures", [source]))).Contains("doesn't allow", StringComparison.Ordinal) && !File.Exists(Path.Combine(pictures, "Recording (2).mp4")), "Turning the setting off stops sends at once");
        hostStore.SetPreferences(hostStore.Preferences with
        {
            FolderAccess = true
        });
        host.EditPeer(hostStore.Peers.Single(p => p.Fingerprint == clientKey.Fingerprint) with
        {
            Paired = false
        });
        Check((await Fails(() => client.BrowseFoldersAsync(hostPeer))).Length > 0, "A device that is no longer paired can't list or send");
    }
}
