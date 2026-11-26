# hopo Android: polish and phone test (2.6.9)

Owner's request: make the Android app as good as possible and test it on the phone over USB; then list next-version ideas for more reliable, faster transfers that do not weigh on the PC.

Found by reading the code
- [ ] Files shared into hopo from another app (Gallery → Share → hopo) are copied off the screen thread, so a large video no longer freezes the app; progress shows while copying.
- [ ] Files picked with Send files are copied in the background with "Preparing file 2 of 5" progress and a larger copy buffer.

Found on the phone
- (added during the test)

Release
- [ ] APK 2.6.9 (version code 7): build, test on the phone (pages, PC → phone and phone → PC transfers with speed, notification Accept, background and screen off, share from another app, rotation and back), copy to dist\.
- [ ] Next-version list for reliability and speed (in the final report).
