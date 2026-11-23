# hopo Windows: send the Explorer selection with a device ring

Part 4 of the [2.6 plan](2026-11-24-hopo-2.6-plan.md). The plan left the shortcut open, so it is a setting: Ctrl+Shift+S by default (plain Shift+S types a capital S and, in Explorer, jumps to files starting with S), Shift+S for those who want it, or Off.

- [ ] A `SendShortcut` preference (Ctrl+Shift+S, Shift+S or Off; default Ctrl+Shift+S). A bad saved value falls back on its own.
- [ ] The key rule lives in Core so it can be tested: S with exactly the chosen modifiers, only while the focus is the file list of a File Explorer window or the desktop, and never while a caret is shown (rename, search, address bar). Every other key press passes through untouched.
- [ ] A keyboard watcher on its own thread (so a busy window never delays typing), in only one running hopo app at a time.
- [ ] Read the selected files and folders of the active Explorer tab, or of the desktop.
- [ ] The ring: a round window at the mouse pointer with the online paired PCs and approved phones around it, numbered, and the item count and file size in the middle. Click a device, press its number, or use the arrow keys and Enter; Esc or clicking elsewhere closes it. Sending works as usual: the receiver still approves.
- [ ] Settings: choose the shortcut or turn it off.
- [ ] Tests for the key rule and the preference.
