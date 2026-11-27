# How hopo is tested

Every hopo version is tested before it's released. Some tests run automatically and some are done by hand on real devices. This page explains the methods and keeps a version-by-version log of what was tested, what was measured, what broke and what was fixed. The newest version is at the top.

Older records: [VERIFICATION.md](../VERIFICATION.md) covers Lantoss 1.9 and earlier. Each change also has a short record in [docs/changes](changes/README.md).

---

## Test methods

| Method | What it checks | How |
|---|---|---|
| **Automated checks** | Pairing, encryption, transfers, file-name safety, sync, history, settings, the phone page | 334 checks in a custom test runner (`.\source\build.ps1 -Mode Test`), plus a browser-page test (`node .\source\tests\browser-ui.test.cjs`). They run on every change. |
| **Screen checks** | Every Windows page in Light and Dark, the Home panels, the Ctrl+Shift+S ring | The app draws each page to an image (`hopoUsers.exe --ui-regression`). Each image is checked for the right content, then looked at. |
| **Break test** | Problem files, cancels, network drops, junk connections, speed, CPU | Two extra hopo engines on one PC send to each other. They are kept off the network, so no real device sees them. |
| **Freeze probe** | Whether the window stops responding during transfers | hopo's real window runs off screen while files arrive. Every 15 ms a background thread times how long the window takes to answer, the same way Windows decides an app is "Not responding". |
| **Phone test** | Every Android screen, transfers both ways, notifications, background, screen off, battery | A real phone (Pixel 7a, Android 17) over USB, with a test PC on the same Wi-Fi. The phone's processor use is read with `top`. Screenshots are taken only while hopo is in front. |
| **Real-window ring test** | The Ctrl+Shift+S ring over File Explorer | A test program opens the ring over a test File Explorer window, then clicks and presses keys like a person would. |

Speeds below were measured on a home network: the phone on 2.4 GHz Wi-Fi (286 Mbps link), the PC on Wi-Fi 6. A 5 GHz network is usually several times faster.

---

## 2.7.0: Send to a folder

**New:** the PC owner adds *drop folders* in Settings. From a phone or another PC, open one of them (or a folder inside it), or make a new folder there, and send straight into it. Only folder names are shown, never files. Devices set to *Always accept* skip the prompt; others are asked.

**Tested:**
- 15 automated checks:
  - nothing is shared until a drop folder is added, and a missing folder can't be added;
  - only the drop folders are listed, once each; a deleted one is left out and doesn't block saving settings;
  - two drop folders with the same name are told apart;
  - no file names and no hidden folders are shown;
  - paths can't leave the drop folders;
  - Decline writes nothing, and Accept lands in exactly the chosen folder;
  - a trusted device skips the prompt;
  - same-name files are kept as copies;
  - a new folder is made only on Accept, one level deep, with a valid name;
  - removing a drop folder stops sends into it at once;
  - an unpaired device is refused.
- A real-window test of the Windows folder browser against a test PC with sample folders (12 of 12 checks): the "no folders shared" message, browsing, Up, New folder (not made before sending), Send here, and the file arriving in the new folder.
- The Windows screen checks, and the Settings drop folder list drawn to an image.
- **Not yet tested on the phone:** the phone was not connected.

## 2.7.0: Android multi-file selection

**Reported:** several files could not be sent from the phone; after choosing several, they weren't shown and couldn't be removed.

**Found:** the phone's file picker already returned several files. The Home Send panel showed only one shortened line and had no remove button.

**Fixed:** every chosen file is listed with its size and a remove button, plus Clear.

**Tested on the phone:** chose 3 files, removed one, and sent 2 to a PC (both verified).

## 2.6.9: Ctrl+Shift+S ring (Windows)

**Reported:** clicking outside the ring or pressing Escape did not close it. The device icons were too loud and the waves too strong.

**Found:**
- Closing depended on Windows giving the ring the keyboard focus. When it didn't, neither outside clicks nor Escape reached the ring.
- The ring window also stayed fully transparent until its first frame was drawn, which could take over a second. Clicks pass through a fully transparent window.

**Fixed:**
- The ring now covers the screen it opens on with an almost invisible layer, so a click anywhere outside the circle closes it. The layer catches clicks from the moment it appears.
- Escape and the number keys go through the shortcut's keyboard hook, so they work with or without focus.
- **New look:**
  - The waves are a thin, faint border of a smaller circle (312 px).
  - Device icons are 40 px: the phone's own picture, or a small PC or phone symbol.
  - Each name sits on its own label.

**Tested:** three runs of the real-window ring test, with all 16 checks passing each time:
- the ring opens over File Explorer;
- a click outside closes it, and a click on the circle itself keeps it open;
- Escape closes it, both with focus and through the hook;
- pressing 1 sends, and so does clicking the device.

## 2.6.9: Android phone test

**Speed**

| Transfer | Before | After |
|---|---|---|
| PC → phone, 500 MB, hopo open | 6.3 MB/s | 9.8 MB/s |
| PC → phone, hopo in the background | — | 10.6 MB/s |
| PC → phone, screen off | — | 14.1 MB/s (wake lock and Wi-Fi lock held, then released) |
| Phone → PC, 200 MB | — | 13.8 MB/s |

**What limited the speed:** the phone's processor, not the Wi-Fi. The encryption library ran AES-GCM in software. ChaCha20-Poly1305 is now offered first: in a benchmark it was 2.7 times faster (403 vs 140 MB/s on a PC, 251 vs 93 MB/s without hardware help). It is just as secure. Older versions still use AES-GCM.

**Processor use with nothing transferring**

| Screen | Before | After |
|---|---|---|
| History | 94% | 15% |
| Home, rings moving | 176% | 95–114% |
| Home, untouched for 20 s | — | 14% |
| hopo in the background | — | 1% |

(100% means one processor core fully busy.)

**What used the battery:** the app redrew the whole screen up to 90 times a second on every page. Now:
- the effects share one capped clock;
- they stop when hopo is in the background;
- they follow Android's *Remove animations* setting;
- the background stays still on a phone, and the rings settle 20 s after the last touch.

**Also found and fixed:**
- History was cut off on a phone: the names and the Delete button were off screen.
- The incoming-files notification covered hopo's own Accept panel.
- *Keep receiving in the background* did nothing on Android.
- Messages said "1 files".
- A large shared video froze the app while it was copied.

**Checked:** every page, pairing, Always accept, sharing from another app, picking files, rotation and the back button.

**Known:** in landscape the Home ring is small.

## 2.6.8: Windows break test and freeze probe

**Broke (now fixed):**
- **One problem file failed the whole send.** This happened with a reserved name such as `aux` or `CON`, a name over 180 characters, a name ending in a dot or space, a file another program had open, or a link inside a folder. Now these are skipped and listed on the transfer, and the rest is sent.
- **Source files were locked while being sent.**
- **Raw system error text** appeared when the network dropped, and the receiver of a cancelled send was told "the other PC disconnected".

**Held up:**
- a 1 GB file at 68–98 MB/s between two engines on one PC;
- 8 transfers at once (97 MB/s);
- cancelling, with no partial files left;
- a network drop and retry (6 s);
- 300 junk connections (all dropped within 20 s);
- unicode names, paths over 260 characters, and empty files and folders.

**Lag found (now fixed):**
- **Disk writes while idle.** The app was idle in the tray but used 25% of one core and rewrote its data file 24 times a minute. Each reply from a paired PC caused a full rewrite.
- **Freezes on the Transfers page.** During 300 short transfers in a row, the window froze for over 100 ms 173 times (worst 275 ms), because every new transfer rebuilt the list.

**After the fixes:**
- Transfers page: 8–37 freezes over 100 ms, worst 136–174 ms.
- Opening Transfers: 96–114 ms (was 150–171).
- Opening Home: 20 ms (was 63).
- 5,000 small files: 30 s (was 48 s).
- Closing the app no longer waits on its final save.

**Not explained:** in two early runs, the window under test stopped answering at the end of a burst. It didn't happen again in four runs on the final code.

## 2.6.6–2.6.7: Android accept and notifications

**Found:** Accept stayed greyed out on the phone. Free space was measured on Android's small system partition instead of the storage where files are saved. The same mistake showed "0 B free".

**Fixed:**
- the free-space check;
- Accept, Decline and Always accept in the notification;
- trusted PCs;
- Delete in Transfers;
- the Text panel reopening after another panel.

**Tested on the phone:**
- accepting from the panel and from the notification;
- Always accept, after which the next file arrived without asking;
- the files appeared in Downloads/hopo.

---

*Next ideas and plans are in [ROADMAP.md](../ROADMAP.md#next-versions).*
