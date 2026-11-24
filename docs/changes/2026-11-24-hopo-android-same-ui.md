# hopo for Android with the same screens as the PC

Asked: an Android app with the same UI as the PC app, connecting and working properly, with a manual connect option. The 2.6.1 APK runs on the owner's Pixel 7a (Android 17) but shows a plain text screen.

Approach: the Mac/Linux app (source/Desktop) already draws the PC design in Avalonia, the toolkit the Android app uses. The Android app now compiles those same screens instead of its own plain one. Tested on the owner's phone over USB (adb), no downloads.

- [x] The shell draws into a plain container (`Shell.Root`) that a desktop window (macOS, Linux) or the Android screen holds. Window-only parts (tray, closing to the tray, window position, screenshots) stay desktop-only.
- [x] Dialogs (connect, compare codes, incoming files, messages, manual connection) open as in-app panels on Android with the same look, since Android has no separate windows. The Back button closes a panel, then the menu, then returns to Home.
- [x] Phone fit: the header keeps the hopo mark, status and menu button; pages use a narrower margin; panels fit the screen width. Pages that cannot work on a phone are left out of the menu and the Home dock: Phone & browser (hosting the phone page), Shared folders, Sync and the Folder panel.
- [x] Android platform parts: picked files are copied from Android's file picker; links open in the browser; notifications use Android notifications; each received transfer goes to its own private folder and is then moved to Downloads/hopo; Reveal points to Downloads/hopo; Share to hopo from other apps opens Send with the shared files or text.
- [x] Manual connect stays on Devices ("Connect manually": the PC's address and pairing code) and is also offered on Home when no PC is found.
- [x] Build the APK as 2.6.5 (version code 3), install it on the phone, check every page with screenshots, and fix what breaks. Copy it to dist\ (replacing the 2.6.1 APK).

Found and fixed while testing on the phone:
- The app closed on start ("hopo keeps stopping", UnsatisfiedLinkError for AndroidApp.n_onCreate) after an incremental build: the generated marshal-method library was out of step with the app code. Marshal methods are now off, and `build.ps1 -Mode Android` always builds from clean. This is the likely cause of the 2.6.0 APK closing on the owner's phone.
- Android 17 always draws apps under the status and gesture bars; the pages, menu and panels are padded by Android's safe area.
- .NET on Android reports Wi-Fi without a usable type and cannot list gateways, so receiving never started ("Connect this phone to a private IPv4 network"). `NetworkAdapter.Available` now finds the phone's wlan/eth addresses by name.
- Storage in the menu measured the system partition (0 B free of 1 GB); it now measures the data partition.
- Two-column pages (Send files, Settings), page headers with buttons, the Text & links send row and the device buttons now stack or wrap on a phone.

Checked on the owner's Pixel 7a (Android 17) over USB:
- Every page opens and fits: Home, the menu, Send files, Text & links, Transfers, Devices, History, Settings, the Connect manually panel and the code-compare panel. Back closes a panel and returns to Home.
- The phone started receiving on Wi-Fi, found the owner's PC (hopo Admin), paired with it after both screens approved the same code, and reconnects by itself after restarting.
- Text sent from the phone arrived in the PC's hopo (Text & links).
- Windows tests: 319 pass. CheckFormat and the Mac/Linux build pass.

Not verified: sending a file from the phone (Android file picker) and receiving a file from the PC into Downloads/hopo. The picker shows the owner's own files, so it was not driven or screenshotted here, and no file was sent from the PC during the test.

APK: dist\hopo-2.6.5-android.apk, 101.4 MB, SHA-256 46265C1572742F722AD0EB3B3E5CF33DE70150BA1B23D2B8EE67DD2150FF2D9E. The 2.6.1 APK was removed.
