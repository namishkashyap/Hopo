# hopo Android: app closes right after opening

Reported: the 2.6.0 APK opens and closes at once. There is no crash log from the phone, so this fixes the causes found in the code and makes the next crash visible.

- [ ] Give Avalonia 12 the first screen the way its Android lifetime expects (a view factory), and keep the old single-view path as a fallback.
- [ ] Use the app's real storage path. On Android, /data/user/0 is a link to /data/data, and hopo's safety check refuses folders reached through links, so the data store could not open.
- [ ] Each new screen (rotation, reopening) hooks up to the engine once and unhooks when it closes, so approvals and refreshes are not doubled.
- [ ] Catch startup and background failures (receiving, the background notification service, unhandled errors), save the details to a crash report, and show it on the next launch with a Copy button.
- [ ] Rebuild the signed APK as 2.6.1 (version code 2) so it installs over 2.6.0, and copy it to dist\.
