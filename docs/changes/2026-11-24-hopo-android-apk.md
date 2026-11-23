# hopo for Android (APK), first version

Part 5 of the hopo 2.6 plan (2026-11-24-hopo-2.6-plan.md). Distribution: APK on GitHub Releases. The one-time tool download was approved ("Start with the APK. Just build an Android").

## Tooling (one time, kept in work\ for offline builds)
- [ ] Copy the installed .NET SDK into work\dotnet (no download, no admin prompt) and install the .NET Android workload there.
- [ ] Download the Android SDK and Java JDK once into work\android-sdk and work\jdk through the workload's InstallAndroidDependencies target.
- [ ] build.ps1 -Mode Android builds a signed release APK into work\android-release. The signing keystore stays in work\android-keystore (outside git) and must be backed up.

## Shared code
- [ ] The Android app compiles Lantoss.Core and the networking code directly. The phone page server (BrowserPortal and BrowserSpaces, which need ASP.NET Core) is replaced on Android by a small stand-in with the same members that never starts a server.
- [ ] The device key uses the existing file-based identity (as on Mac and Linux).

## Android app (source/Android, Avalonia)
- [ ] Starts receiving on the home Wi-Fi and finds nearby PCs (holding a multicast lock while discovering).
- [ ] Connect to a PC with the matching-code comparison, exactly like PC to PC.
- [ ] Send files to a paired PC (picked files are copied to the app's cache, sent, then removed).
- [ ] Receive files after approving them; finished files are copied to Downloads/hopo so they appear in Files and the gallery.
- [ ] Send and read text and links.
- [ ] "Share to hopo" from any app's share menu.
- [ ] Transfers list with progress.
- [ ] A notification keeps hopo receiving while it is in the background.

## Verification
- [ ] Windows tests still pass (build.ps1 -Mode Test, CheckFormat).
- [ ] The APK builds, is signed, and has the expected package name, version, permissions and native libraries.
- [ ] Not verified on a real phone unless noted here.
