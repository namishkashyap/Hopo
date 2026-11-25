# hopo Windows: fixes from the break test (lag, idle work, failed sends)

A break test on Windows (two extra engines on this PC, hopo's window off screen, and the running hopo Admin watched without changes) found where sends fail and why the app lags. Everything found is fixed here.

Sends that break
- [x] One problem file no longer fails the whole send: reserved names (aux, CON), names over 180 characters, names ending in a dot or space, files another program holds, and links or junctions are skipped, the rest is sent, and the sender is told what was skipped and why.
- [x] Files being sent are read without locking them, so they can still be saved, renamed or deleted (a file that changes mid-send fails its checksum as before).
- [x] Clear messages: the receiver is told the sender cancelled (not "the other PC disconnected"), and network errors read as plain sentences instead of raw .NET text.
- [x] Many small files: no copy of the whole file list for every file (a lookup by path instead).
- [x] "Pairing was declined" / "Too many pairing attempts" during the test: not a hopo fault. The test engines had automatic connection on and tried to connect to the real PC and phone on the Wi-Fi, which declined; the tests now run with it off.

Idle work and lag
- [x] metadata.json: saves are combined and written on a background writer; a reply from a paired PC no longer rewrites the file (24 rewrites a minute while idle); transfers save at most every 10 s plus at start and finish. Clicks (Favorite, Trust, Rename, theme, settings) no longer wait for disk.
- [x] Device checks raise "devices changed" only when something visible changed; the Explorer menu is rewritten only when the paired PCs change.
- [x] Transfers page keeps its rows and updates them in place instead of rebuilding all of them on every change; search typing is batched; open file details are not rebuilt every refresh.
- [x] Devices, Send and Shared Spaces rebuild only when their devices or spaces change (no lost clicks or scroll jumps every few seconds).
- [x] Home's Phone panel is rebuilt only when phones or approvals change, and its QR code is drawn once.
- [x] The firewall check runs once per start and when the network changes, not on every phone event, and never retries a cancelled permission prompt in a loop.
- [x] Animations pause while the window is hidden or minimised (Home's device pulses and ripples kept running in the tray).
- [x] Approval dialogs show on their own with a taskbar button when hopo is hidden in the tray, instead of an invisible dialog that makes hopo look frozen.
- [x] Folder sync and shared spaces: one transfer entry per pass instead of one per file.
- [x] Per-chunk work during transfers: the trust check and the timeout token are not rebuilt for every 64 KB.
- [x] Free-space reading for the status line runs off the UI thread.

Release
- [x] Tests, CheckFormat, Mac/Linux build; re-run the break test and the freeze probe; Windows 2.6.8 MSIs in dist\ (keeping the last two versions).

Checked: Windows tests 319 pass; UI regression checks pass; CheckFormat and the Mac/Linux build pass; both 2.6.8 MSIs pass verify-installer.

Break test again on the fixed code:
- A folder with aux, CON.txt, a 244-character name, names ending in a dot or space and a junction now sends everything else and lists what was left out; a single locked file says "Nothing could be sent. locked.bin: another program is using it".
- A source file can be deleted or appended to while it is sent; the receiver still gets the bytes that were checksummed.
- The receiver of a cancelled send reads "The transfer stopped: the other PC cancelled it or the connection was lost"; a dropped network reads "The connection to the other PC was lost …". Retry after a drop completed in 5.7 s.
- 5,000 small files: 30 s (was 48 s).
- Freeze probe with Transfers open during 300 back-to-back transfers: 8–37 stalls over 100 ms and worst 136–174 ms over four runs (was 173 stalls, worst 275 ms). Opening Transfers 96–114 ms (was 150–171), Home 20 ms (was 63).
- Closing: the final save first waited on the UI thread for work that continues on the UI thread (4.5 s at exit); fixed so the flush runs off the UI thread.

Not explained: two early runs of the freeze probe stopped answering on the UI thread at the end of a burst while all hopo locks were free. It did not happen again in four runs on the final code, with or without dispatcher tracing. If hopo ever stops responding, note what was on screen.

MSIs in dist: hopo-Users-Setup-2.6.8-x64.msi (SHA-256 689EFB7EE8654F539B9F0E985F770D983424D0CF196B72CBBF03204E76BEFD49) and hopo-Admin-Setup-2.6.8-x64.msi (SHA-256 477AFE052F5C859DBF12E10E542082BCDF3C46DA3221870F4CFE569729715C42). The 2.6.4 MSIs were removed.
