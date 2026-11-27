# Send straight into a folder on another PC

Asked for: from a phone or PC, see the other PC's folders, pick one, and send files straight into it. It is separate from Shared Spaces.

Decided with the user: only the PC owner's user folders are open (Desktop, Documents, Downloads, Pictures, Videos, Music and their subfolders). It is off until the owner turns it on. Devices set to Always accept skip approval; others are asked.

- [ ] Setting on the receiving PC (Windows app, and Mac/Linux): "Let paired devices send into my folders", off by default.
- [ ] Engine: list folders (names only, no file names) and send a batch into the chosen folder over the existing pinned TLS. Paired devices only; checked again before writing. Paths can't leave the user folders, and links are refused.
- [ ] Windows app: "Send to a folder…" on the Send page with a folder browser.
- [ ] Phone and Mac/Linux app: "Send to a folder…" on the Home Send panel and the Send page with the same browser.
- [ ] Tests: off by default, browse, send, trusted skip, approval, path escape, revoked pairing.
- [ ] Version 2.7.0: Windows MSIs and APK in dist; phone test.
- [ ] docs/TESTING.md: a 2.7.0 entry.

## Revised: drop folders (asked for after the first version)

The user asked for something simpler: the owner picks the folders to share, and anyone sending from a phone or PC sees only those, can make a new folder inside one, and sends into it. Receiving on the phone is not part of this; the user wants phone → PC and PC → PC.

- [ ] Settings (Windows app, and Mac/Linux): a "Drop folders" list with Add folder and Remove. It replaces the "all my user folders" switch. Nothing is shared until a folder is added.
- [ ] Engine: only the drop folders and their subfolders are listed and accepted. A sender may send into a new folder inside one; it is created only once the files are accepted, and only one level deep. The same name rules apply.
- [ ] Folder browser (Windows, phone, Mac/Linux): a "New folder" box inside a drop folder; clear messages when the PC shares no folders.
- [ ] Tests: drop folders only, a new folder is created on Accept and not on Decline, bad names and two-level new folders are refused, removing a drop folder stops sends.
- [ ] Rebuild the 2.7.0 MSIs and APK; real-window test of the Windows browser; phone test when the phone is connected; update docs/TESTING.md.
