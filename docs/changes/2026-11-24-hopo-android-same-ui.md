# hopo for Android with the same screens as the PC

Asked: an Android app with the same UI as the PC app, connecting and working properly, with a manual connect option. The 2.6.1 APK runs on the owner's Pixel 7a (Android 17) but shows a plain text screen.

Approach: the Mac/Linux app (source/Desktop) already draws the PC design in Avalonia, the toolkit the Android app uses. The Android app now compiles those same screens instead of its own plain one. Tested on the owner's phone over USB (adb), no downloads.

- [ ] The shell draws into a plain container (`Shell.Root`) that a desktop window (macOS, Linux) or the Android screen holds. Window-only parts (tray, closing to the tray, window position, screenshots) stay desktop-only.
- [ ] Dialogs (connect, compare codes, incoming files, messages, manual connection) open as in-app panels on Android with the same look, since Android has no separate windows. The Back button closes a panel, then the menu, then returns to Home.
- [ ] Phone fit: the header keeps the hopo mark, status and menu button; pages use a narrower margin; panels fit the screen width. Pages that cannot work on a phone are left out of the menu and the Home dock: Phone & browser (hosting the phone page), Shared folders, Sync and the Folder panel.
- [ ] Android platform parts: picked files are copied from Android's file picker; links open in the browser; notifications use Android notifications; each received transfer goes to its own private folder and is then moved to Downloads/hopo; Reveal points to Downloads/hopo; Share to hopo from other apps opens Send with the shared files or text.
- [ ] Manual connect stays on Devices ("Connect manually": the PC's address and pairing code) and is also offered on Home when no PC is found.
- [ ] Build the APK as 2.6.5 (version code 3), install it on the phone, check every page with screenshots, and fix what breaks. Copy it to dist\ (replacing the 2.6.1 APK).
