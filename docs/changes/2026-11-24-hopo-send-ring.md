# hopo Windows: send the Explorer selection with a device ring

Part 4 of the [2.6 plan](2026-11-24-hopo-2.6-plan.md). The plan left the shortcut open, so it is a setting: Ctrl+Shift+S by default (plain Shift+S types a capital S and, in Explorer, jumps to files starting with S), Shift+S for those who want it, or Off.

- [x] A `SendShortcut` preference (Ctrl+Shift+S, Shift+S or Off; default Ctrl+Shift+S). A bad saved value falls back on its own.
- [x] The key rule lives in Core so it can be tested: S with exactly the chosen modifiers, only while the focus is the file list of a File Explorer window or the desktop, and never while a caret is shown (rename, search, address bar). Every other key press passes through untouched.
- [x] A keyboard watcher on its own thread (so a busy window never delays typing), in only one running hopo app at a time.
- [x] Read the selected files and folders of the active Explorer tab, or of the desktop.
- [x] The ring: a round window at the mouse pointer with the online paired PCs and approved phones around it, numbered, and the item count and file size in the middle. Click a device, press its number, or use the arrow keys and Enter; Esc or clicking elsewhere closes it. Sending works as usual: the receiver still approves.
- [x] Settings: choose the shortcut or turn it off.
- [x] Tests for the key rule and the preference.

Checked: Test (318 pass, 12 new), CheckFormat, Build, and the UI screenshot checks (30 pass, one new for the ring). The selection reader was run against this PC's open Explorer windows and desktop and read their selections.
Not verified: pressing the shortcut in Explorer on a real desktop (the hook and the ring window opening at the pointer) was not tried here, to avoid typing into the user's open windows.
