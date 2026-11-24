# hopo Android: accept from the notification, trusted PCs, delete transfers, bigger Home labels

Reported on the phone: incoming files cannot be accepted; receiving lags while the app is closed; the notification has no Accept or Decline; there is no way to trust a PC so files arrive without asking; Transfers cannot be deleted; the phone's name in the middle of Home and the PC names on the ring are small.

- [x] Accept works: the free-space check measured Android's small system partition, so Accept stayed greyed out (and the engine refused the files). Free space is now measured on the receive folder's own partition on Android, macOS and Linux.
- [x] The incoming-files notification has Decline, Accept and Always accept. It shows whether or not hopo is open; the in-app panel and the notification close together.
- [x] Trusted PCs: "Always accept from this PC" in the incoming panel and the notification, and "Always accept files" / "Ask every time" on Devices. A trusted PC's files arrive without asking, each transfer in its own folder, then move to Downloads/hopo.
- [x] Receiving in the background: while a transfer runs, hopo keeps the phone's processor and Wi-Fi awake (Android otherwise slows Wi-Fi and the app when the screen is off or hopo is closed).
- [x] Less work while files move: page refreshes from transfers and discovery are batched to at most four a second (shared with Mac and Linux).
- [x] Transfers and History: a Delete button on each finished transfer (received files stay).
- [x] Home on the phone: the phone's name in the centre and the PC names on the ring are larger.
- [x] Build the APK as 2.6.6 (version code 4). Test on the phone: a test sender on this PC pairs with the phone and sends a file; accept from the panel and from the notification; check the file in Downloads/hopo. Copy to dist\ replacing 2.6.5.

Checked: the APK builds (2.6.6, version code 4); Windows tests 319 pass; CheckFormat and the Mac/Linux build pass. A test sender (hopo's own engine running as "Test PC" on this PC) is ready to pair with the phone and send a file.
Not verified on the phone: the phone stayed locked during this session, so the panel and notification Accept, Always accept, the Delete button and the larger Home names were not yet tried on screen. The free-space cause was confirmed in the code (the same partition mistake made the menu show "0 B free of 1 GB").

APK: dist\hopo-2.6.6-android.apk, 101.4 MB, SHA-256 8D7FC319BF3A7EA9E3960C9F94DB1793C24CDEE38DF20FF3B2447F08777B59BB. The 2.6.5 APK was removed.
