# Drop folders get their own menu page (2.7.1)

Reported: the Drop folders setting couldn't be found. Found: on Windows, Settings is hidden in the Users app (admin only), and on the phone the setting is hidden because a phone only sends. Asked for: a separate, clear "Drop folders" menu instead of a setting, in the Windows app and the APK, as a new version 2.7.1.

- [ ] Windows app: a "Drop folders" page in the Share menu, shown in both the Users and Admin apps. Top: "Folders on this PC that others can send into" with Add drop folder and Remove. Below: "Send into a folder on another device", listing paired devices, each with "Choose files and send…".
- [ ] Phone and Mac/Linux app: the same "Drop folders" page in the Share menu. On the phone only the sending part is shown, with a line explaining that phones send into a PC's drop folders.
- [ ] Remove the Drop folders section from both Settings pages.
- [ ] Version 2.7.1 (Android version code 9); rebuild MSIs and APK into dist; remove the 2.7.0 builds.
- [ ] Checks: tests, format, screen render of the new page, real-window folder browser test, Android build; docs/TESTING.md entry.
