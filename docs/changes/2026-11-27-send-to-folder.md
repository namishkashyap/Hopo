# Send straight into a folder on another PC

Asked for: from a phone or PC, see the other PC's folders, pick one, and send files straight into it. It is separate from Shared Spaces.

Decided with the user: only the PC owner's user folders are open (Desktop, Documents, Downloads, Pictures, Videos, Music and their subfolders). It is off until the owner turns it on. Devices set to Always accept skip approval; others are asked.

- [x] Setting on the receiving PC (Windows app, and Mac/Linux): "Let paired devices send into my folders", off by default.
- [x] Engine: list folders (names only, no file names) and send a batch into the chosen folder over the existing pinned TLS. Paired devices only; checked again before writing. Paths can't leave the user folders, and links are refused.
- [x] Windows app: "Send to a folder…" on the Send page with a folder browser.
- [x] Phone and Mac/Linux app: "Send to a folder…" on the Home Send panel and the Send page with the same browser.
- [x] Tests: off by default, browse, send, trusted skip, approval, path escape, revoked pairing.
- [x] Version 2.7.0: Windows MSIs and APK in dist. (Phone test: see below.)
- [x] docs/TESTING.md: a 2.7.0 entry.

## Revised: drop folders (asked for after the first version)

The user asked for something simpler: the owner picks the folders to share, and anyone sending from a phone or PC sees only those, can make a new folder inside one, and sends into it. Receiving on the phone is not part of this; the user wants phone → PC and PC → PC.

- [x] Settings (Windows app, and Mac/Linux): a "Drop folders" list with Add folder and Remove. It replaces the "all my user folders" switch. Nothing is shared until a folder is added.
- [x] Engine: only the drop folders and their subfolders are listed and accepted. A sender may send into a new folder inside one; it is created only once the files are accepted, and only one level deep. The same name rules apply.
- [x] Folder browser (Windows, phone, Mac/Linux): a "New folder" box inside a drop folder; clear messages when the PC shares no folders.
- [x] Tests: drop folders only, a new folder is created on Accept and not on Decline, bad names and two-level new folders are refused, removing a drop folder stops sends.
- [x] Rebuild the 2.7.0 MSIs and APK; real-window test of the Windows browser; update docs/TESTING.md.
- [ ] Phone test: not done yet, the phone was not connected.

## Result

- The first version (all user folders behind one switch) was replaced before release; it never shipped.
- 334 automated checks pass (15 for this feature). The Windows folder browser real-window test passes 12 of 12. The screen checks, the format check and the Android build pass. Both MSIs pass verify-installer.
- dist: hopo-Users-Setup-2.7.0-x64.msi (D6FE5423B7CDC23D3A0753BB24F7A8ED47DD16C01922237FD1ED66E3FB2D9E24), hopo-Admin-Setup-2.7.0-x64.msi (E801905CB9E5ED8BBCDD1B532A3EA483105560704EDE957466D50374F880F6CD), hopo-2.7.0-android.apk (83C0E6D179D14F1D656636F42141DB646DA126238C650EEE98FA3BB448011C73).
