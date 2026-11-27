using System.Security.Authentication;
using Lantoss.Core;

namespace Lantoss.Networking;

/// <summary>
/// Send to a folder: a paired device lists the folders under this PC's user folders and sends a batch straight into one. Off until the owner turns on <see cref="Preferences.FolderAccess"/>. Only folder names are listed, never file names; every request and the final write are checked again.
/// </summary>
public sealed partial class TransferEngine
{
    /// <summary>The top-level folders another device may send into, by name. Tests replace it.</summary>
    public Func<(string Name, string Path)[]> FolderRoots { get; set; } = UserFolders;
    public static (string Name, string Path)[] UserFolders()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        (string Name, string Path)[] all = [("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)), ("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)), ("Downloads", home.Length == 0 ? "" : Path.Combine(home, "Downloads")), ("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)), ("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)), ("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))];
        return all.Where(root => root.Path.Length > 0).ToArray();
    }
    /// <summary>Lists the subfolders of <paramref name="folder"/> on another PC ("" lists its user folders). Older versions reply with an update message.</summary>
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
    /// <summary>Sends files and folders into a folder chosen with <see cref="BrowseFoldersAsync"/>. The other PC asks first unless it trusts this device.</summary>
    public Task SendToFolderAsync(Peer peer, string folder, IEnumerable<string> paths, CancellationToken token = default)
    {
        FileCatalog.ValidateRelative(folder);
        return UploadBatchAsync(peer, "folder-upload", "", folder, paths, token);
    }
    private async Task ServeFoldersAsync(Stream stream, Peer peer, WireMessage hello, CancellationToken token)
    {
        string relative = hello.RelativePath;
        string Folder()
        {
            if (!store.Preferences.FolderAccess)
                throw new ProtocolException(store.Preferences.DeviceName + " doesn't allow sending into its folders. Turn it on in hopo Settings on that PC.");
            if (store.Trusted(peer.Fingerprint) == null)
                throw new AuthenticationException("The PC's access was revoked.");
            if (relative.Length == 0)
                return "";
            FileCatalog.ValidateRelative(relative);
            int slash = relative.IndexOf('/');
            string top = slash < 0 ? relative : relative[..slash];
            (string Name, string Path) root = FolderRoots().FirstOrDefault(item => item.Name == top);
            if (root.Path is null)
                throw new ProtocolException("This folder isn't available.");
            string folder = slash < 0 ? Path.GetFullPath(root.Path) : FileCatalog.Within(root.Path, relative[(slash + 1)..]);
            try
            {
                FileCatalog.CheckSafePath(folder);
            }
            catch (IOException) { throw new ProtocolException("Links and cloud-only folders can't be opened."); }
            if (!Directory.Exists(folder))
                throw new ProtocolException("This folder is no longer available.");
            return folder;
        }
        string target = Folder();
        if (hello.Kind == "folder-browse")
        {
            List<SpaceEntry> entries = [];
            if (target.Length == 0)
                foreach ((string name, string path) in FolderRoots())
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
            throw new ProtocolException("Choose a folder inside Desktop, Documents, Downloads, Pictures, Videos or Music.");
        Wire.Require(hello, "folder-upload");
        void Guard()
        {
            if (Folder() != target)
                throw new OperationCanceledException("Sending into this folder was turned off.");
        }
        await ReceiveSpaceBatchAsync(stream, peer, relative, "", target, store.Trusted(peer.Fingerprint)?.Trusted == true, hello, Guard, token);
    }
}
