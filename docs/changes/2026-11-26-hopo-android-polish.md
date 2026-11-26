# hopo Android: polish and phone test (2.6.9)

Owner's request: make the Android app as good as possible and test it on the phone over USB; then list next-version ideas for more reliable, faster transfers that do not weigh on the PC.

Found by reading the code
- [x] Files shared into hopo from another app (Gallery → Share → hopo) are copied off the screen thread, so a large video no longer freezes the app; progress shows while copying.
- [x] Files picked with Send files are copied in the background with "Preparing file 2 of 5" progress and a larger copy buffer.

Found on the phone
- [x] History on a phone: each entry as two lines (name and status; direction, device, size, time, folder and Delete). The seven-column table squeezed the names and Delete off screen.
- [x] Phone wording: Send files says "Add files to send"; Settings says "Start receiving automatically when hopo opens" and "Keep receiving in the background when I leave hopo", and that setting now works (off: the background service stops when hopo leaves the screen).
- [x] While hopo is on screen, the incoming-files notification goes quietly into the shade instead of covering hopo's own Accept panel.
- [x] Faster, lighter encryption on every platform: ChaCha20-Poly1305 is offered first (2.7× faster than AES-GCM in this TLS library: 403 vs 140 MB/s on a PC, 251 vs 93 without hardware help). AES-GCM stays for 2.6.8 and older.
- [x] Battery: the phone redrew the screen up to 90 times a second on every page. Effects now share one 30 fps clock (Home rings 15 fps on a phone), stop while hopo is in the background, respect Android's Remove animations, and on a phone the background glows and card sweeps stay still, the rings draw lighter (60 points, no outer glow, faded by brush), static parts are cached, and the rings settle 20 s after the last touch.
- [x] Shared pages (phone, Mac, Linux) rebuild only on change, like Windows: Transfers keeps its cards, History, Devices and Shared folders skip unchanged refreshes, filter pills restyle only on change.
- [x] "1 file", not "1 files", in notifications, the incoming panel and the ready message (all platforms); a failed share says why.

Release
- [x] APK 2.6.9 (version code 7): build, test on the phone (pages, PC → phone and phone → PC transfers with speed, notification Accept, background and screen off, share from another app, rotation and back), copy to dist\.
- [x] Next-version list for reliability and speed (in the final report).

Measured on the Pixel 7a (2.4 GHz Wi-Fi, 286 Mbps link) against a test PC on this computer:
- PC → phone 500 MB: 6.3 MB/s before; 9.8 MB/s with ChaCha20 and lighter drawing; 10.6 MB/s with hopo in the background; 14.1 MB/s with the screen off (wake lock and Wi-Fi lock held, released afterwards).
- Phone → PC 200 MB (picked with Add files, copied in about 4 s): 13.8 MB/s including approval.
- CPU while idle: History 94% → 15%; Home 176% while moving → 95–114% moving, 14% once settled; background 1%.
- Checked: every page, pairing, the quiet in-app prompt, Always accept, rotation, back button. 319 Windows tests, CheckFormat, Mac/Linux build.
- Not fixed here: in landscape the Home ring is small.

APK: disthopo-2.6.9-android.apk, SHA-256 0ce9b12b0ccf6cfbea416bcc4b89336daa18c9d19ce1e9c982fd0385bf705ee8.
