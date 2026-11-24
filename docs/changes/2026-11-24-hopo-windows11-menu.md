# hopo Windows: "Send with hopo" in the first Windows 11 right-click menu

Part 3 of the [2.6 plan](2026-11-24-hopo-2.6-plan.md). Decided: a self-signed package (free; setup asks for administrator approval once per PC to trust it), and a one-time download of the build tools.

- [ ] Download the build tools named in SETUP.md into work\explorer-tools, checking their SHA-256: w64devkit 2.10.1 (g++, windres) and Microsoft.Windows.SDK.BuildTools 10.0.26100.1742 (MakeAppx, SignTool).
- [ ] Create a new private signing key in work\explorer-signing (the 2.3 key is gone; never committed, must be backed up like the Android keystore) and commit its public certificate, source\Explorer\Publisher.cer. Subject stays CN=Lantoss Development.
- [ ] Native menu (source\Explorer): titled "Send with hopo" (hopo Admin: "Send with hopo Admin") with a submenu of "Choose a device…" and each paired PC. It reads the app's own cached device list (Desktop or AdminDesktop key) and starts the app installed next to it (hopoUsers.exe or hopoAdmin.exe). Each app has its own COM class, so both can be installed. The native tests are updated.
- [ ] One package per app (Lantoss.ExplorerIntegration for hopo Users, Lantoss.ExplorerIntegration.Admin for hopo Admin), built from one manifest template, version 2.6.3.0, signed with the new key.
- [ ] App side: register the package for the app that is running, write the device list under that app's key, and use the classic right-click entry whenever the package is not registered (Windows 10, or trust declined).
- [ ] Installer: ship the menu DLL, the app's package and its logos; register the package during setup when "Share in Explorer" is chosen (one administrator prompt to trust the publisher; declining keeps the classic entry and setup still finishes); remove it when the feature or app is removed.
- [ ] Build hopo 2.6.3 (version files, CHANGELOG), both MSIs, verify them, and copy them to dist\, keeping the last two Windows versions (2.6.2 and 2.6.3).
