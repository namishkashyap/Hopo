# Roadmap

## Next versions

These ideas come from testing hopo on real devices (see [docs/TESTING.md](docs/TESTING.md)). The aims: more reliable transfers, faster transfers, and keeping the PC light while files move. Each item says what we saw that led to it.

### Faster transfers

1. **Save received files straight into Downloads on the phone.** Today a file is written to hopo's private folder, then copied to Downloads/hopo, so every byte is written twice.
2. **Send picked files directly.** A file chosen on the phone is copied into hopo's cache before it is sent, which doubles the wait and the storage used for big videos.
3. **Use the phone's built-in encryption.** Phones have hardware-accelerated encryption. hopo's own encryption library still takes about one processor core for 10–14 MB/s on a Pixel 7a.
4. **Pack many small files into one stream.** Each file costs about 5 ms today, so 10,000 photos take around 50 seconds even on a fast network.
5. **Use parallel streams for big files on fast Wi-Fi** (5 GHz and Wi-Fi 6), where one stream can't fill the link.
6. **Tell people when the phone is on 2.4 GHz Wi-Fi.** The test phone's 2.4 GHz link capped transfers near 14 MB/s. 5 GHz is usually several times faster.

### More reliable

7. **Resume from where it stopped.** After a Wi-Fi drop, a transfer starts again from the beginning. It could continue from the last file and block that was checked.
8. **Retry automatically when Wi-Fi drops,** with a short wait between tries, instead of asking the sender to press Retry.
9. **Find a PC whose port changed, right away.** When a PC moves to a different port, the phone can keep the old one and show the PC as offline until discovery catches up. When the saved port doesn't answer, hopo should try the usual port and ask the network again.
10. **Tidy up duplicate devices.** When a PC gets a new key under the same name, the old entry stays as a second, offline device. hopo could offer to remove the old one.
11. **Make Home use the space in landscape.** On a phone turned sideways, the Home ring becomes small.

### Light on the PC

12. **Run transfers at lower priority,** and limit how many run at once, so the PC stays smooth while large files move. The faster encryption in 2.6.9 already lowers the PC's processor use.
13. **Clean up old firewall rules when installing,** and never add one for Public networks. Test PCs had dozens of leftover rules from older versions, and one allowed hopo on Public networks.

---

# Delivery status

## Local sharing improvements in 1.9

Automatic share-wollvo.local on port 80 with conflict probing, per-device names,
common-name owner handoff and browser PC picker. Friendly names require working
mDNS on the local network; port conflicts or unsupported resolvers use the direct
QR fallback. No DNS/router/hosts-file changes are made. Physical Wi-Fi/phone
acceptance remains outstanding; see VERIFICATION.md.

Shared-folder Refresh stays in the current folder in app/browser and preserves
upload choices. Text & links adds a bounded local board, native TLS sends and
phone-browser posting, with literal rendering and explicit copy/open actions.
Idle sync reuses manifests and filesystem events, performs minute metadata
sweeps and rolling old-file audits, and bounds hashes/caches. Manual Sync now
forces checksum verification; conflict and previous-version protections remain.

## Installer and desktop integration in 1.8

Per-user MSI with Start menu, optional desktop/startup features, repair/uninstall;
native Windows 11 main-menu device submenu backed by a signed sparse package;
single-instance batch handoff; tray receiving/sync and Start server/Exit. Home
now focuses on files, devices and phone access. The development publisher requires
explicit trust during setup. Public publisher signing, a machine-wide service
and silent auto-updates are not included.

## Included in Lantoss for Windows 1.9

Lantoss branding across the app and browser; teal/mint Light and Dark themes;
immediate System/Light/Dark appearance selection in the Windows sidebar and
browser header; live system preference following; existing-installation data
compatibility; explanatory source comments and a developer architecture guide.

Home, Send, Transfers, Devices, Phone & browser, History, Settings; local mDNS;
direct PC connection by default with saved public keys; optional access codes;
native TLS and checksum-verified file/folder transfer; receiver approval;
progress/cancellation/retry; duplicate policies; local metadata and themes.

Phone/browser support: local HTTP page, QR link, explicit PC-file downloads,
approved streaming uploads with progress, optional code/session access control,
CSRF/host checks, duplicate keep-both, and separate HTTP history labels. Browser
HTTP does not claim native encryption or sender-checksum verification.

Shared Spaces: persistent local folder registrations, read-only by default,
optional approved uploads into existing subfolders, real browser browsing and
downloads, availability checks, and revocation on stop/remove/permission change.
Removing a space preserves its files. Permissions apply to all browser sessions.
The top bar and each space have Open in browser buttons. Native/browser transfer
views show measured speed and estimated remaining time; browser completion
records include measured averages.

Native Shared Spaces: browse another paired PC's shared folders inside the app,
download verified files, and send an approved upload batch into its chosen folder.
Automatic two-way sync: persisted links and one-time per-peer/space grants,
five-second reconciliation, offline retries, restart recovery, checksum revision
checks, conflict copies on both PCs, previous-version backups, and pause/remove/
revoke controls. Deletions do not propagate; backups are not pruned automatically.

Connection automation: dedicated UDP LAN discovery alongside mDNS, automatic
direct pairing with an opt-out and optional-code enforcement, authenticated
remembered-peer probes, address fallback without changing the key pin, and
adapter-change recovery. A native diagnostic view opens Windows network settings;
it opens Windows settings and reports automatic firewall-setup status.

Private-network firewall setup: automatic rules after Windows UAC approval,
exact executable and actual TCP ports, UDP discovery ports, Private/local-subnet
scope, idempotent updates, previous app-denial repair preserving Public/Domain
blocks, cancellation deferral/retry, and a settings opt-out. No global firewall,
network-category, router, other-application, or managed-policy changes.
PC dropdowns render only names, honor custom display templates, and remain compact.

Legacy comparison pairing/manual code fallback remains available. Discovery and
browser receiving can start on opening; both have user controls. Actual tests
and physical-device limitations are in VERIFICATION.md.

## Not implemented

Pause/resume and restart continuation; recipient batches/priorities/bandwidth
controls; global auto-accept of manual incoming files; compression; folder archives
for browser download.

Per-person team roles, invites, audit administration, deletion propagation,
scheduled sync, backup pruning/restore UI, block-level sync, and NAS/mounted shares.

HTTPS setup for ordinary phone browsers, expiring internet share links, remote
relays, SSO, organizations, code signing, and native macOS/Linux clients.
No automatic router exposure or certificate-warning bypass is provided.
