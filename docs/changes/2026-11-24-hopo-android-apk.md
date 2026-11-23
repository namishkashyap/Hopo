# hopo for Android (APK), first version

Part 5 of the hopo 2.6 plan (2026-11-24-hopo-2.6-plan.md). Distribution: APK on GitHub Releases. The one-time tool download was approved ("Start with the APK. Just build an Android").

## Tooling (one time, kept for offline builds)
- [x] Install the .NET Android workload. Changed from the plan: this .NET installs workloads system-wide (into C:\Program Files\dotnet), so the work\dotnet copy was not used and was deleted.
- [x] Download the Android SDK (436 MB) and Java JDK (302 MB) once into work\android-sdk and work\jdk through the workload's InstallAndroidDependencies target.
- [x] build.ps1 -Mode Android builds a signed release APK into work\android-release. The signing key is created once in work\android-keystore (outside git) and must be backed up.

## Shared code
- [x] The Android app compiles Lantoss.Core and the networking code directly. The phone page server (BrowserPortal and BrowserSpaces, which need ASP.NET Core) is replaced on Android by PhonePageStub.cs, which has the same members and never starts a server.
- [x] The device key uses the existing file-based identity (as on Mac and Linux).

## Android app (source/Android, Avalonia 12)
- [x] Starts receiving on the home Wi-Fi and finds nearby PCs, holding a multicast lock while discovering.
- [x] Connect to a PC with the matching-code comparison (or its access code), exactly like PC to PC.
- [x] Send files to a paired PC. Picked files are copied to the app's cache with safe names, sent, then removed.
- [x] Receive files after approving them. Finished files move to Downloads/hopo, so they appear in Files and the gallery.
- [x] Send and copy text and links.
- [x] "Share to hopo" from any app's share menu, then choose the PC.
- [x] Transfers list with progress and Cancel. The screen refreshes at most a few times a second.
- [x] A "hopo is ready" notification with a Stop action keeps hopo receiving in the background.
- [x] If startup fails, the reason is shown on screen instead of the app closing.

## Verification
- [x] Windows tests still pass (build.ps1 -Mode Test), plus CheckFormat.
- [x] APK built and signed (apksigner verify). Package io.github.namishkashyap.hopo, version 2.6.0 (code 1), min Android 10 (API 29), target API 36, native code arm64-v8a, armeabi-v7a, x86_64. Permissions: Internet, network and Wi-Fi state, Wi-Fi multicast, foreground service (data sync), notifications.
- [x] APK 97.5 MB (untrimmed, 3 architectures). SHA-256 4341da0a2c30fcca1aa37e46f91e73f8b48299602fc1663eefe163ff822de8e9. A copy is at dist\hopo-2.6.0-android.apk.
- [ ] Not verified on a real phone or emulator: pairing, sending, receiving, the share menu and background receiving still need a test on a device.
