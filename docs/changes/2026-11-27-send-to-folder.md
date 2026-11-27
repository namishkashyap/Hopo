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
