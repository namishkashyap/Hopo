# hopo Android: app closes right after opening

Reported: the 2.6.0 APK opens and closes at once. There is no crash log from the phone, so this fixes the causes found in the code and makes the next crash visible.

- [x] Give Avalonia 12 the first screen the way its Android lifetime expects (a view factory), and keep the old single-view path as a fallback. The 2.6.0 code only set the single-view screen, which the Android lifetime may never read, so the activity had no screen.
- [x] Use the app's real storage path. On Android, /data/user/0 is a link to /data/data, and hopo's safety check refuses folders reached through links, so the data store could not open.
- [x] Each new screen (rotation, reopening) hooks up to the engine while shown and unhooks when closed. Moving finished files to Downloads and removing sent copies are hooked once for the whole app.
- [x] Catch failures in startup, receiving, the background notification service and the screen; save the details to a crash report (crash.txt in the app's private folder); and show it on the next launch with Copy details and Dismiss. The background service now starts as a plain service, so Android cannot kill the app if it refuses the notification.
- [x] Rebuild the signed APK as 2.6.1 (version code 2), so it installs over 2.6.0, and copy it to dist\hopo-2.6.1-android.apk. Size 98.0 MB, SHA-256 31fe56d35e43292cfbbe2642e85b033c27d93cd2c0089922cc6c1a8d041118df. The broken 2.6.0 APK was removed from dist\.

Checked: the APK builds and is signed; the generated manifest has the application class, exported launcher activity, HopoTheme, and the dataSync service type. CheckFormat passes.
Not verified: still not run on a phone or emulator. If it still closes, the crash report on the next launch, or `adb logcat` over USB (adb is already in work\android-sdk\platform-tools), will show the exact error.
