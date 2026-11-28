using System.Security.Authentication;
using Lantoss.Core;

namespace Lantoss.Networking;

/// <summary>
/// Send to a folder: a paired device lists this PC's drop folders (<see cref="Preferences.DropFolders"/>) and their subfolders by name and sends a batch straight into one, or into a new folder one level inside one. Nothing is shared until the owner adds a drop folder. Only folder names are listed, never file names; every request and the final write are checked again.
/// </summary>
public sealed partial class TransferEngine
{
    /// <summary>The drop folders by the name other devices see: the folder's own name, with " (2)" and so on for repeats.</summary>
    public static (string Name, string Path)[] DropRoots(IEnumerable<string>? folders)
    {
        List<(string Name, string Path)> roots = [];
        foreach (string folder in folders ?? [])
        {
            string full = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), name = Path.GetFileName(full), shown = name;
            try
            {
                FileCatalog.ValidateSegment(name);
            }
            catch (InvalidDataException) { continue; }
            for (int i = 2; roots.Any(root => string.Equals(root.Name, shown, StringComparison.OrdinalIgnoreCase)); i++)
                shown = name + " (" + i + ")";
            roots.Add((shown, full));
        }
        return [.. roots];
    }
    /// <summary>Lists the subfolders of <paramref name="folder"/> on another PC ("" lists its drop folders). Older versions reply with an update message.</summary>
    public async Task<SpaceEntry[]> BrowseFoldersAsync(Peer peer, string folder = "", CancellationToken token = default)
    {
        WireMessage result = await SpaceRequestAsync(peer, new()
        {
            Kind = "folder-browse",
            RelativePath = folder
        }, token);
        Wire.Require(result, "entries");
        foreach (SpaceEntry entry in result.Entries)
        {
            if (entry is null || entry.Name is null || entry.Path is null || !entry.Directory)
                throw new ProtocolException("Invalid folder listing.");
            FileCatalog.ValidateRelative(entry.Path);
            FileCatalog.ValidateSegment(entry.Name);
        }
        return result.Entries;
    }
    /// <summary>Sends files and folders into a folder chosen with <see cref="BrowseFoldersAsync"/>, or into a new folder inside it, which the other PC makes once it accepts. It asks first unless it trusts this device.</summary>
    public Task SendToFolderAsync(Peer peer, string folder, IEnumerable<string> paths, CancellationToken token = default)
    {
        FileCatalog.ValidateRelative(folder);
        return UploadBatchAsync(peer, "folder-upload", "", folder, paths, token);
    }
    /// <summary>Makes a folder, and any missing folders above it, inside a drop folder on another PC right away. The other PC asks first unless it trusts this device. False when it declines.</summary>
    public async Task<bool> CreateFolderAsync(Peer peer, string folder, CancellationToken token = default)
    {
        FileCatalog.ValidateRelative(folder);
        WireMessage result = await SpaceRequestAsync(peer, new()
        {
            Kind = "folder-create",
            RelativePath = folder
        }, token);
        if (result.Kind == "declined")
            return false;
        Wire.Require(result, "accepted");
        return true;
    }
    private async Task ServeFoldersAsync(Stream stream, Peer peer, WireMessage hello, CancellationToken token)
    {
        string relative = hello.RelativePath;
        // An upload or a new folder may end in folders that don't exist yet; the closest folder above them that exists must be inside the drop folder.
        string Folder(bool upload)
        {
            (string Name, string Path)[] roots = DropRoots(store.Preferences.DropFolders);
            if (roots.Length == 0)
                throw new ProtocolException(store.Preferences.DeviceName + " hasn't shared any folders yet. On that PC, open hopo Settings and add a drop folder.");
            if (store.Trusted(peer.Fingerprint) == null)
                throw new AuthenticationException("The PC's access was revoked.");
            if (relative.Length == 0)
                return "";
            FileCatalog.ValidateRelative(relative);
            int slash = relative.IndexOf('/');
            string top = slash < 0 ? relative : relative[..slash];
            (string Name, string Path) root = roots.FirstOrDefault(item => item.Name == top);
            if (root.Path is null)
                throw new ProtocolException("This folder is no longer shared.");
            string folder = slash < 0 ? root.Path : FileCatalog.Within(root.Path, relative[(slash + 1)..]), existing = folder;
            if (upload && slash >= 0)
                while (!Directory.Exists(existing) && existing.Length > root.Path.Length)
                    existing = Path.GetDirectoryName(existing)!;
            try
            {
                FileCatalog.CheckSafePath(folder);
            }
            catch (IOException) { throw new ProtocolException("Links and cloud-only folders can't be opened."); }
            if (!Directory.Exists(existing) || File.Exists(folder))
                throw new ProtocolException("This folder is no longer available.");
            return folder;
        }
        string target = Folder(hello.Kind is "folder-upload" or "folder-create");
        if (hello.Kind == "folder-browse")
        {
            List<SpaceEntry> entries = [];
            if (target.Length == 0)
                foreach ((string name, string path) in DropRoots(store.Preferences.DropFolders))
                {
                    try
                    {
                        FileCatalog.CheckSafePath(path);
                        if (Directory.Exists(path))
                            entries.Add(new(name, name, true, 0));
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
                }
            else
                foreach (DirectoryInfo child in new DirectoryInfo(target).EnumerateDirectories())
                {
                    if (entries.Count >= 1000)
                        break;
                    if ((child.Attributes & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) != 0 || child.Name.StartsWith('.'))
                        continue;
                    try
                    {
                        FileCatalog.ValidateSegment(child.Name);
                    }
                    catch (InvalidDataException) { continue; }
                    entries.Add(new(child.Name, relative + "/" + child.Name, true, 0));
                }
            await Wire.WriteAsync(stream, new()
            {
                Kind = "entries",
                Entries = [.. entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)]
            }, token);
            return;
        }
        if (target.Length == 0)
            throw new ProtocolException("Open one of the shared folders first, then send.");
        if (hello.Kind == "folder-create")
        {
            bool made = Directory.Exists(target);
            if (!made)
            {
                string parent = target;
                while (!Directory.Exists(parent))
                    parent = Path.GetDirectoryName(parent)!;
                Manifest shown = new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Entries = [new() { RelativePath = Path.GetRelativePath(parent, target).Replace('\\', '/'), Directory = true }]
                };
                using CancellationTokenSource approval = CancellationTokenSource.CreateLinkedTokenSource(token);
                approval.CancelAfter(TimeSpan.FromMinutes(2));
                made = store.Trusted(peer.Fingerprint)?.Trusted == true || ApproveReceive != null && (await ApproveReceive(new(peer.Name + " → " + relative, peer.Fingerprint, shown, parent, FileCatalog.FreeSpace(parent), peer.KeyVerified, false, true), approval.Token)).Accepted;
                if (made)
                    Guard();
            }
            await Wire.WriteAsync(stream, new()
            {
                Kind = made ? "accepted" : "declined"
            }, token);
            return;
        }
        Wire.Require(hello, "folder-upload");
        void Guard()
        {
            if (Folder(true) != target)
                throw new OperationCanceledException("This folder is no longer shared.");
            Directory.CreateDirectory(target);
        }
        await ReceiveSpaceBatchAsync(stream, peer, relative, "", target, store.Trusted(peer.Fingerprint)?.Trusted == true, hello, Guard, token);
    }
}
