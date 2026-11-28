# Trusted devices send into drop folders without asking (2.7.4)

Date: 2026-11-28

Every send into a drop folder asked for approval on the Windows PC, because Windows had no Always accept button and trust was hidden on Devices as "Trust".

## Items

- [x] Windows incoming-files window: an "Always accept from <device>" button for paired devices (not browsers). It accepts these files and trusts the device for next time.
- [x] Mac/Linux incoming-files window: show the same button for drop-folder and shared-folder sends too.
- [x] Drop folders page (Windows, Mac/Linux): a "Who can send without asking" list of paired devices, each with Always accept / Ask every time.
- [x] Windows Devices page: the trust button reads "Always accept files" / "Ask every time", matching the other apps.
- [x] Version 2.7.4 for Windows (Users and Admin MSIs) and Android; tests, screen checks and a send check.
- [x] README: list the latest version of every app.
- [x] Clean up: keep only the last two versions in dist, and remove old build output and logs from work (keep the offline caches and signing keys).

## Results

- 338 automated checks, screen checks and CheckFormat pass. Windows harness, real app receiving into a drop folder: 9 of 9.
- Users and Admin MSIs pass verify-installer; Mac and Linux packages pass their checks; APK 2.7.4 (version code 12) is installed on the Pixel 7a.
- dist keeps 2.7.4 plus the version before it for each app (2.7.3 Android, 2.7.2 Windows, 2.6.8 Mac/Linux). Build output, logs and old verification folders were removed from work; caches and signing keys were kept.
