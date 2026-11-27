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
        Check(off.Contains("hasn't shared any folders yet", StringComparison.Ordinal), "Nothing is shared until the receiving PC adds a drop folder");
        string music = Path.Combine(hostStore.Root, "Music");
        Directory.CreateDirectory(music);
        Check((await Fails(() => Task.Run(() => hostStore.SetPreferences(hostStore.Preferences with { DropFolders = [Path.Combine(hostStore.Root, "Missing")] })))).Contains("can't be found", StringComparison.Ordinal), "A folder that doesn't exist can't be added as a drop folder");
        hostStore.SetPreferences(hostStore.Preferences with
        {
            DropFolders = [documents, pictures, music, documents + Path.DirectorySeparatorChar]
        });
        Directory.Delete(music);
        hostStore.SetPreferences(hostStore.Preferences with
        {
            DeviceName = "Folder host"
        });
        SpaceEntry[] roots = await client.BrowseFoldersAsync(hostPeer);
        Check(hostStore.Preferences.DropFolders.Length == 3 && roots.Select(entry => entry.Name).SequenceEqual(["Documents", "Pictures"]), "The top level lists only the drop folders, once each; a deleted one is left out and doesn't block saving settings");
        Check(TransferEngine.DropRoots([documents, Path.Combine(outside, "Documents")]).Select(root => root.Name).SequenceEqual(["Documents", "Documents (2)"]), "Two drop folders with the same name are told apart");
        SpaceEntry[] inside = await client.BrowseFoldersAsync(hostPeer, "Documents");
        Check(inside.Length == 1 && inside[0].Name == "Work" && inside[0].Path == "Documents/Work" && inside[0].Directory, "Browsing lists subfolder names only: no file names and no hidden folders");
        Check((await Fails(() => client.BrowseFoldersAsync(hostPeer, "Documents/../Outside"))).Length > 0 && (await Fails(() => client.BrowseFoldersAsync(hostPeer, "Outside"))).Length > 0, "Paths can't leave the drop folders or name another folder");
        int approvals = 0;
        string? asked = null;
        host.ApproveReceive = (request, _) => { approvals++; asked = request.DefaultFolder; return Task.FromResult(new ReceiveDecision(false, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Reports", [source]);
        Check(approvals == 1 && asked == Path.Combine(documents, "Work", "Reports") && !File.Exists(Path.Combine(documents, "Work", "Reports", "Recording.mp4")), "A device that isn't trusted is asked first, with the chosen folder shown; Decline writes nothing");
        host.ApproveReceive = (request, _) => { approvals++; return Task.FromResult(new ReceiveDecision(true, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Reports", [source]);
        Check(approvals == 2 && File.ReadAllText(Path.Combine(documents, "Work", "Reports", "Recording.mp4")) == "recording bytes", "After Accept the file lands in exactly the chosen folder");
        string fresh = Path.Combine(documents, "Work", "Recordings 27 Nov");
        host.ApproveReceive = (request, _) => { approvals++; return Task.FromResult(new ReceiveDecision(false, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Recordings 27 Nov", [source]);
        Check(approvals == 3 && !Directory.Exists(fresh), "A new folder isn't made when the files are declined");
        host.ApproveReceive = (request, _) => { approvals++; return Task.FromResult(new ReceiveDecision(true, request.DefaultFolder)); };
        await client.SendToFolderAsync(hostPeer, "Documents/Work/Recordings 27 Nov", [source]);
        Check(approvals == 4 && File.ReadAllText(Path.Combine(fresh, "Recording.mp4")) == "recording bytes" && (await client.BrowseFoldersAsync(hostPeer, "Documents/Work")).Any(entry => entry.Name == "Recordings 27 Nov"), "A new folder inside a drop folder is made on Accept and holds the files");
        Check((await Fails(() => client.SendToFolderAsync(hostPeer, "Documents/New/Deeper", [source]))).Length > 0 && !Directory.Exists(Path.Combine(documents, "New")) && (await Fails(() => client.SendToFolderAsync(hostPeer, "Documents/CON", [source]))).Length > 0 && (await Fails(() => client.SendToFolderAsync(hostPeer, "Documents", [source]))).Length == 0 && (await Fails(() => client.SendToFolderAsync(hostPeer, "New top", [source]))).Length > 0, "A new folder is one level deep inside a drop folder and must have a valid name");
        approvals = 4;
        Peer clientPeer = hostStore.Peers.Single(p => p.Fingerprint == clientKey.Fingerprint);
        host.EditPeer(clientPeer with
        {
            Trusted = true
        });
        await client.SendToFolderAsync(hostPeer, "Pictures", [source]);
        Check(approvals == 4 && File.Exists(Path.Combine(pictures, "Recording.mp4")), "A device set to Always accept sends without a prompt");
        await client.SendToFolderAsync(hostPeer, "Pictures", [source]);
        Check(File.Exists(Path.Combine(pictures, "Recording (1).mp4")), "A same-name file is kept as a copy, not overwritten");
        hostStore.SetPreferences(hostStore.Preferences with
        {
            DropFolders = [documents]
        });
        Check((await Fails(() => client.SendToFolderAsync(hostPeer, "Pictures", [source]))).Contains("no longer shared", StringComparison.Ordinal) && !File.Exists(Path.Combine(pictures, "Recording (2).mp4")), "Removing a drop folder stops sends into it at once");
        host.EditPeer(hostStore.Peers.Single(p => p.Fingerprint == clientKey.Fingerprint) with
        {
            Paired = false
        });
        Check((await Fails(() => client.BrowseFoldersAsync(hostPeer))).Length > 0, "A device that is no longer paired can't list or send");
    }
}
