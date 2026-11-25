# hopo

**Send files, folders, text and links between your own computers and phones over your home Wi-Fi. No cloud, no account, no internet needed.**

hopo (formerly Lantoss) is a free, open-source app for Windows, macOS, Linux and Android. Files go straight from one device to another on your own network, encrypted, and nothing is uploaded anywhere.

**Latest version: 2.6.8.** All downloads are on the **[Releases page](https://github.com/namishkashyap/Lantoss/releases/latest)**.

---

## What hopo does

- **Send files and whole folders** from one device to another on the same Wi-Fi or home network. Large files are fine, and every file is checked with a SHA-256 checksum when it arrives.
- **You stay in control.** The receiver sees who is sending what and chooses **Accept** or **Decline**. For your own devices you can choose **Always accept**.
- **Encrypted, pinned connections.** Devices pair once by comparing the same code on both screens. After that, every connection is encrypted (TLS) and checked against the paired device's key.
- **Phones without an app.** Any phone or tablet (iPhone included) can open hopo's page in its web browser by scanning a QR code, then send and receive files.
- **Android app.** hopo on Android has the same screens as the computer app. Incoming files can be accepted straight from the notification and are saved to **Downloads/hopo**.
- **Text and links.** Copy a note or a link on one device and open it on another.
- **Shared folders and two-way sync.** Share a folder with your other devices, or keep two folders in sync automatically.
- **Windows extras.** You can right-click files in File Explorer and choose **Send with hopo**. You can also press **Ctrl+Shift+S** in File Explorer to open a ring of your devices and send with one click.
- **Private by design.** No account, no server and no tracking. hopo works only on your local network and never changes your router or DNS.

---

## Download

Pick the file for your device. Each link downloads the file directly from the [2.6.8 release](https://github.com/namishkashyap/Lantoss/releases/tag/v2.6.8).

| Device | File to download | Size |
|---|---|---|
| **Windows 10 / 11** (most people) | [hopo-Users-Setup-2.6.8-x64.msi](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-Users-Setup-2.6.8-x64.msi) | 75 MB |
| **Windows 10 / 11** (the person who manages devices and settings) | [hopo-Admin-Setup-2.6.8-x64.msi](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-Admin-Setup-2.6.8-x64.msi) | 75 MB |
| **Mac with Apple Silicon** (M1, M2, M3, M4) | [hopo-2.6.8-macOS-AppleSilicon.zip](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-macOS-AppleSilicon.zip) | 57 MB |
| **Mac with an Intel processor** | [hopo-2.6.8-macOS-Intel.zip](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-macOS-Intel.zip) | 60 MB |
| **Android phone or tablet** (Android 10 or later) | [hopo-2.6.8-android.apk](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-android.apk) | 101 MB |
| **Linux** (Ubuntu, Debian, Mint, Pop!_OS; 64-bit PC) | [hopo_2.6.8_amd64.deb](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo_2.6.8_amd64.deb) | 60 MB |
| **Linux on ARM** (Raspberry Pi OS 64-bit and similar) | [hopo_2.6.8_arm64.deb](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo_2.6.8_arm64.deb) | 57 MB |
| **Any other Linux** (Fedora, Arch, openSUSE…) | [x64 .tar.gz](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-linux-x64.tar.gz) · [arm64 .tar.gz](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-linux-arm64.tar.gz) | 57–60 MB |
| **iPhone, iPad or any phone without the app** | Nothing to download: see [Phones without the app](#phones-without-the-app) | — |

Install hopo on every device you want to share between, and keep them on the **same Wi-Fi or home network**.

> These builds are not signed by Microsoft, Apple or Google, so each system asks you to confirm the first time. The steps below show exactly what to click.

---

## Windows

### Which file do I need?

hopo for Windows comes as two apps. Most people need **hopo Users**.

| App | File | What it has |
|---|---|---|
| **hopo Users** | `hopo-Users-Setup-2.6.8-x64.msi` | Home, Send files, Text & links and Transfers. For everyone who just shares files. |
| **hopo Admin** | `hopo-Admin-Setup-2.6.8-x64.msi` | Everything in hopo Users, plus Devices, shared folders, Sync, History and Settings. For the person who manages the devices. |

Prefer not to install? [hopoUsers.exe](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopoUsers.exe) and [hopoAdmin.exe](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopoAdmin.exe) run on their own without setup.

### How to install

1. Click the download link for your app in the table above. Your browser saves the `.msi` file to your **Downloads** folder.
   - If the browser warns that the file "isn't commonly downloaded", choose **Keep** (in Edge: **…** → **Keep** → **Keep anyway**).
2. If an older hopo or Lantoss is running, right-click its icon in the taskbar tray (near the clock) and choose **Exit**.
3. Open **Downloads** and double-click the `.msi` file.
   - If a blue **Windows protected your PC** screen appears, click **More info**, then **Run anyway**.
4. Follow the setup. Under **Custom Setup** you can add a desktop shortcut and **Launch at sign-in**.
5. When Windows asks for administrator approval to add **Send with hopo** to the right-click menu, choose **Yes**. Choose **No** to keep it under *Show more options*.
6. Start **hopo Users** or **hopo Admin** from the Start menu.

### First start

- If hopo offers to set up **Private-network** firewall access, approve the Windows prompt. This lets your other devices find this PC.
- Make sure your home network is set to **Private**: **Settings → Network & internet → Wi-Fi** (or Ethernet) **→** your network **→ Private network**.

### Update or uninstall

- **Update:** download the newer `.msi` and run it. It upgrades in place and keeps your settings, pairings and history.
- **Uninstall:** **Settings → Apps → Installed apps → hopo → Uninstall**. Files you received stay where they are.

---

## Mac

Requires **macOS 14 Sonoma or later**.

### Which file do I need?

Click the **Apple menu** (top-left corner) → **About This Mac**:
- **Chip: Apple M1, M2, M3 or M4** → download [hopo-2.6.8-macOS-AppleSilicon.zip](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-macOS-AppleSilicon.zip)
- **Processor: Intel** → download [hopo-2.6.8-macOS-Intel.zip](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-macOS-Intel.zip)

### How to install

1. Click the download link for your Mac. Safari saves the zip to **Downloads**, and usually unpacks it for you. If it doesn't, double-click the zip.
2. Open the unpacked folder. It has `hopo.app`, **Install hopo.command** and `install.sh`.
3. Double-click **Install hopo.command**.
   - If macOS says it **can't verify** the file, click **Done**. Then open **System Settings → Privacy & Security**, scroll down, click **Open Anyway** next to "Install hopo.command", and confirm.
   - A Terminal window opens. It copies hopo to **Applications**, clears the download warning and prepares the app for this Mac. It may ask for your Mac password.
4. Open **hopo** from **Applications** or Launchpad.

### First start

- Click **Allow** when macOS asks whether hopo may accept **incoming network connections**.
- Click **Allow** when macOS asks for **Local Network** access (macOS 15 and later). If you declined, turn it on in **System Settings → Privacy & Security → Local Network → hopo**.

### Uninstall

Quit hopo, then move **hopo** from **Applications** to the Bin.

---

## Android

Requires **Android 10 or later**. The app isn't on the Play Store yet, so you install the APK file directly.

### How to install

1. On your phone, open this page in **Chrome** and tap [hopo-2.6.8-android.apk](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-android.apk). Chrome downloads the file (about 101 MB).
   - If Chrome warns that "this type of file can harm your device", tap **Download anyway**.
2. When the download finishes, tap **Open** (or open **Files → Downloads** and tap `hopo-2.6.8-android.apk`).
3. If Android says **"For your security, your phone is not allowed to install unknown apps from this source"**, tap **Settings**, turn on **Allow from this source**, and go back.
4. Tap **Install**.
   - If **Google Play Protect** shows a warning, tap **More details → Install anyway**. hopo is open source, and the warning appears for any app that isn't from the Play Store.
5. Tap **Open**. When hopo asks to send you **notifications**, tap **Allow**, so you can accept incoming files from the notification.

### First start

- Connect the phone to the **same Wi-Fi** as your computers. Your computers running hopo appear on the phone's Home screen.
- Tap a computer and choose **Connect**. Both screens show the same code; approve on both if they match.
- Files you receive are saved in **Downloads/hopo**, where your Files and Gallery apps can see them.
- A computer can't be found automatically? Open the menu → **Devices → Manual connection**, and type the address shown on the computer.

### Update or uninstall

- **Update:** download the newer APK and install it over the old one. Your pairings are kept.
- **Uninstall:** press and hold the hopo icon → **Uninstall**. Files in Downloads/hopo stay.

---

## Linux

hopo needs a desktop session (X11, or Wayland with XWayland).

### Ubuntu, Debian, Linux Mint, Pop!_OS (.deb)

Download [hopo_2.6.8_amd64.deb](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo_2.6.8_amd64.deb) (or [hopo_2.6.8_arm64.deb](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo_2.6.8_arm64.deb) for ARM), then in a terminal:

```sh
cd ~/Downloads
sudo apt install ./hopo_2.6.8_amd64.deb      # or ./hopo_2.6.8_arm64.deb
```

Start **hopo** from your applications menu, or run `hopo`. Uninstall with `sudo apt remove hopo`.

### Any other distribution (.tar.gz)

Download [hopo-2.6.8-linux-x64.tar.gz](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-linux-x64.tar.gz) (or the [arm64 version](https://github.com/namishkashyap/Lantoss/releases/download/v2.6.8/hopo-2.6.8-linux-arm64.tar.gz)), then:

```sh
cd ~/Downloads
tar -xzf hopo-2.6.8-linux-x64.tar.gz
cd hopo-2.6.8
./install.sh            # for your user; or: sudo ./install.sh for everyone
```

Uninstall with `./uninstall.sh`.

If a firewall such as `ufw` is on, allow hopo on your home network:

```sh
sudo ufw allow from 192.168.0.0/16 to any port 48585:48588 proto tcp
sudo ufw allow from 192.168.0.0/16 to any port 5353,48587 proto udp
```

---

## Phones without the app

iPhones, iPads and any phone or tablet can use hopo without installing anything:

1. On a computer running hopo, open **Phone & browser** (on Home, click **Phone**).
2. Scan the QR code with the phone's camera and open the link.
3. The page is encrypted with your computer's own certificate, so the browser warns once. Choose **Show details → Visit this website** (iPhone) or **Advanced → Proceed** (Android).
4. On the computer, click **Allow** for the new phone.

The phone can now send files to the computer, download files the computer shares with it, and exchange text and links.

---

## Using hopo

1. **Install hopo** on two devices on the same Wi-Fi and open it on both.
2. **Connect them once.** The other device appears on Home. Click or tap it and choose **Connect**. Both screens show the same code; approve on both if they match.
3. **Send.** Click **Send files**, choose files or a folder, and pick the device. On Windows you can also right-click files in File Explorer → **Send with hopo**, or select them and press **Ctrl+Shift+S**.
4. **Receive.** The receiving device shows who is sending what. Choose **Accept**, **Decline** or **Always accept**. Received files go to **Downloads/Lantoss Inbox** on computers (you can change this in Settings) and to **Downloads/hopo** on Android.

More detail on every platform, including firewall and uninstall steps, is in **[INSTALL.md](INSTALL.md)**. The full feature notes and history are in **[docs/FEATURES-AND-HISTORY.md](docs/FEATURES-AND-HISTORY.md)**, and the change log is in [CHANGELOG.txt](CHANGELOG.txt).

---

## Questions

- **Does it need the internet?** No. Your devices only need to be on the same network.
- **Is it safe?** Every connection is encrypted and pinned to the device you paired with, and nothing is sent until the receiver accepts (unless they chose **Always accept** for that device). The code is open for anyone to read.
- **Why does my computer or phone warn me when installing?** The builds aren't signed with a paid Microsoft, Apple or Google certificate, so each system asks you to confirm once. The steps above show what to click.
- **Older versions?** Every version is on the [Releases page](https://github.com/namishkashyap/Lantoss/releases). All versions from 2.0.2 on can pair with each other.

---

## For developers

hopo is written in C# on .NET 10. It uses WPF on Windows, Avalonia on macOS, Linux and Android, and ASP.NET Core Kestrel for the phone page. To build it yourself, see [SETUP.md](SETUP.md) and [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md).

## License

hopo is released under the [MIT License](LICENSE).
