# Keyboard, Paste button and folders from the phone (2.7.2)

Asked for: on the phone the text box hides behind the keyboard; a Paste button; and making folders on a PC from the phone or another PC. Agreed with the user: all four, as 2.7.2.

- [x] Phone keyboard: when the keyboard opens, the page moves up by its height and the box being typed in scrolls into view; it moves back when the keyboard closes. All pages.
- [x] Paste button: next to the Text & links box (phone, Mac/Linux and Windows) and the New folder name box in the folder browser. Says "Nothing to paste" when the clipboard has no text.
- [x] Create folder right away: a "Create folder" button in the folder browser makes the folder on the PC without sending files. Folders inside folders in one go (for example Projects/2026/November). Same approval as files: Always accept devices go straight in, others are asked first; the PC's prompt says a folder will be made.
- [x] Whole folder from the phone: "Choose a folder" on the phone opens Android's folder picker; hopo copies the folder with its subfolders and sends it as one item.
- [x] Tests for the engine parts; checks; version 2.7.2 with MSIs and APK in dist; phone test; docs/TESTING.md entry.

## Result

- 338 automated checks pass (4 new). CheckFormat, the Windows screen checks and the Windows folder browser test (12/12) pass. The Android and Mac/Linux builds pass, and both MSIs pass verify-installer.
- **Phone test (Pixel 7a, test PC with sample folders):**
  - the keyboard lift works on the Text panel, Text & links and the folder browser;
  - Paste works;
  - Create folder made Pictures/Phone/2026;
  - a phone folder with a subfolder arrived complete.
  - Afterwards: Test PC was unpaired, the test files were removed, and stay-on was turned off.
- Found during the phone test and fixed:
  - the Home dock covered the Share button while typing;
  - after the phone was locked and unlocked, the keyboard lift followed the old window;
  - the status line kept "Preparing…" after a folder was copied.
- dist:
  - hopo-Users-Setup-2.7.2-x64.msi (F2751FF4B693AA139393CC2EAEDB635BF4A7A6B1C8465AB94C762F8F1F2E1081)
  - hopo-Admin-Setup-2.7.2-x64.msi (F81A52AB826E443FB14AE56AD4155E13C24FE39A019F55519991E64F3CC2B752)
  - hopo-2.7.2-android.apk (CFE8F7AE1D98E784E52125BDB4F2A5EBB11B9FBD45F2135064B7FF83B94C696E, version code 10)
  - The 2.6.9 builds were removed (the last two versions are kept).
