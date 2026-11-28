# Trusted devices send into drop folders without asking (2.7.4)

Date: 2026-11-28

Every send into a drop folder asked for approval on the Windows PC, because Windows had no Always accept button and trust was hidden on Devices as "Trust".

## Items

- [ ] Windows incoming-files window: an "Always accept from <device>" button for paired devices (not browsers). It accepts these files and trusts the device for next time.
- [ ] Mac/Linux incoming-files window: show the same button for drop-folder and shared-folder sends too.
- [ ] Drop folders page (Windows, Mac/Linux): a "Who can send without asking" list of paired devices, each with Always accept / Ask every time.
- [ ] Windows Devices page: the trust button reads "Always accept files" / "Ask every time", matching the other apps.
- [ ] Version 2.7.4 for Windows (Users and Admin MSIs) and Android; tests, screen checks and a send check.
- [ ] README: list the latest version of every app.
- [ ] Clean up: keep only the last two versions in dist, and remove old build output and logs from work (keep the offline caches and signing keys).
