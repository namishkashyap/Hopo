# hopo Android: accept from the notification, trusted PCs, delete transfers, bigger Home labels

Reported on the phone: incoming files cannot be accepted; receiving lags while the app is closed; the notification has no Accept or Decline; there is no way to trust a PC so files arrive without asking; Transfers cannot be deleted; the phone's name in the middle of Home and the PC names on the ring are small.

- [ ] Accept works: the free-space check measured Android's small system partition, so Accept stayed greyed out (and the engine refused the files). Free space is now measured on the receive folder's own partition on Android, macOS and Linux.
- [ ] The incoming-files notification has Decline, Accept and Always accept. It shows whether or not hopo is open; the in-app panel and the notification close together.
- [ ] Trusted PCs: "Always accept from this PC" in the incoming panel and the notification, and "Always accept files" / "Ask every time" on Devices. A trusted PC's files arrive without asking, each transfer in its own folder, then move to Downloads/hopo.
- [ ] Receiving in the background: while a transfer runs, hopo keeps the phone's processor and Wi-Fi awake (Android otherwise slows Wi-Fi and the app when the screen is off or hopo is closed).
- [ ] Less work while files move: page refreshes from transfers and discovery are batched to at most four a second (shared with Mac and Linux).
- [ ] Transfers and History: a Delete button on each finished transfer (received files stay).
- [ ] Home on the phone: the phone's name in the centre and the PC names on the ring are larger.
- [ ] Build the APK as 2.6.6 (version code 4). Test on the phone: a test sender on this PC pairs with the phone and sends a file; accept from the panel and from the notification; check the file in Downloads/hopo. Copy to dist\ replacing 2.6.5.
