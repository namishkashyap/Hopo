# Drop folders get their own menu page (2.7.1)

Reported: the Drop folders setting couldn't be found. Found: on Windows, Settings is hidden in the Users app (admin only), and on the phone the setting is hidden because a phone only sends. Asked for: a separate, clear "Drop folders" menu instead of a setting, in the Windows app and the APK, as a new version 2.7.1.

- [x] Windows app: a "Drop folders" page in the Share menu, shown in both the Users and Admin apps. Top: "Folders on this PC that others can send into" with Add drop folder and Remove. Below: "Send into a folder on another device", listing paired devices, each with "Choose files and send…".
- [x] Phone and Mac/Linux app: the same "Drop folders" page in the Share menu. On the phone only the sending part is shown, with a line explaining that phones send into a PC's drop folders.
- [x] Remove the Drop folders section from both Settings pages.
- [x] Version 2.7.1 (Android version code 9); rebuild MSIs and APK into dist; remove the 2.7.0 builds.
- [x] Checks: tests, format, screen render of the new page, real-window folder browser test, Android build; docs/TESTING.md entry.

## Result

- Windows screen checks pass. The menu check and the Users-app page list now include Drop folders, and the new page is checked in Light and Dark.
- 334 automated checks, CheckFormat, the Android build and the Windows folder browser test (12/12) pass. Both MSIs pass verify-installer.
- dist: hopo-Users-Setup-2.7.1-x64.msi (7BA9C7B321DA13BD3FCA6B15BF2FF6C2359E0708C788D26C9FEDDDA355772422), hopo-Admin-Setup-2.7.1-x64.msi (E188EFCBEF36AAC66932BE5E82E662EEDF18663B2CA500146F06F6FC4DB8B753), hopo-2.7.1-android.apk (1A695CA69C7736A8A18D429E1755BE2BCE2CA87D0F4B0F322F6C9B73CEE51ED7, version code 9). The 2.7.0 builds were removed.
- Phone test still to do (phone not connected).
