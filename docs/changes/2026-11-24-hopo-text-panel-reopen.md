# hopo: the Text panel opens again after another panel

Reported on the phone: Text, then Send files, then Text again shows an error ("the TextBox already has a parent StackPanel") and the Text panel does not open. The message box is kept between openings so a draft survives, but the previous panel still held it.

- [x] Home's Text panel takes the message box out of the previous panel before adding it (shared with Mac and Linux).
- [x] APK 2.6.7 (version code 5, also bringing the Home speed card): build, test Text → Send files → Text on the phone, copy to dist\ replacing 2.6.6.

Checked on the phone (Pixel 7a): Text, Send files, Text again and Text closed and reopened all open the Text panel with no error.

APK: dist\hopo-2.6.7-android.apk, 101.4 MB, SHA-256 0EBAFD9DC5CF4C7CF23A59D3DB227D72CF7B59FBAD2C68ABCD06C2CAA249CE63. The 2.6.6 APK was removed.
