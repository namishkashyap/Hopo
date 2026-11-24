# hopo Windows: waving send ring, sending without opening hopo, speed card on Home

Reported on the PC: the Ctrl+Shift+S ring has no visible motion and does not look like the Home ring; sending from it brings the hopo window forward; there is no small notice that the files are on their way; the speed line under the Home dock appears and disappears, so the dock jumps up and down.

- [x] Send ring design: the Home page's waving, spiralling coloured rings (shared drawing code, the same colours and motion), a glass centre like Home, and a smaller ring overall.
- [x] Sending from the ring does not open hopo: focus goes back to File Explorer (or the desktop) when the ring closes.
- [x] A small hopo toast at the bottom right says "Sending to <device>" with what is sent; it never takes focus and fades out by itself. The receiving PC is asked as before.
- [x] Home: the transfer speed moves out of the dock into a fixed card at the bottom left (direction, device, speed), so nothing else moves when it appears or changes. Same on the Mac, Linux and Android Home.
- [x] Windows 2.6.7: tests, format check, publish, and the Users and Admin MSIs in dist\ (keeping the last two versions).

Checked: Windows tests 319 pass; UI regression checks pass (Send-ring.png shows the waving rings, glass centre and smaller size; Home unchanged); CheckFormat and the Mac/Linux build pass; both 2.6.7 MSIs pass verify-installer.
Not verified live: the ring's focus hand-back and the toast need the installed app (the running hopo Admin was left alone). Android gets the speed card with its next APK build.

MSIs in dist\: hopo-Users-Setup-2.6.7-x64.msi (SHA-256 DBF93FF569F99C60E33779E5DA738BF93A88A5F2C5F76081BFD43E4690C56787) and hopo-Admin-Setup-2.6.7-x64.msi (SHA-256 EF5B48BE973AE01E54924DEC5F3201C4CB8BE533A2C502FA1DE41DF0CC6737B2). The 2.6.3 MSIs were removed.
