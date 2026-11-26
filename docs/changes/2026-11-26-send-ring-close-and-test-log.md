# Send ring closes reliably and looks calmer; public test log and roadmap

Owner's request: the Ctrl+Shift+S ring does not close when clicking outside or pressing Escape; its device icons are too loud and the wavy lines too strong. Make the waves a faint border of the circle, the circle and icons smaller, names clear, and show the device's picture in its icon. Also publish the testing done in each version and the list of next ideas as documents linked from the README.

Send ring (Windows)
- [x] Clicking anywhere outside the ring closes it: the ring covers the screen it opens on with an almost invisible layer, so the click reaches hopo even when Windows did not give the ring the focus.
- [x] Escape closes the ring and 1–9 send to that device, caught by the shortcut's keyboard hook while the ring is open (they used to need the focus too).
- [x] Calmer look: the waving lines become a thin, faint border around the circle; the circle is smaller (312 px); devices sit on a faint inner orbit.
- [x] Smaller device icons (40 px): the phone's picture when it has one, otherwise a small PC or phone icon in the device's colour (no big bold letter), a thin edge and a soft glow; the number badge is smaller.
- [x] Names are clear: each name sits on its own dark label under the icon.
- [x] Test: UI check screenshot, the ring opened over File Explorer in a real hopo window, click outside, Escape, number key and click to send.

Documents
- [x] docs/TESTING.md: what was tested in each version and how (methods, devices, measured results), from the break test, the Windows freeze probe and the Android phone test.
- [x] ROADMAP.md: a new "Next versions" section at the top with the next-version ideas for reliability, speed and keeping the PC light.
- [x] README: a "Testing and roadmap" section linking both.

Release
- [x] Windows 2.6.9 MSIs (dist\, last two versions kept).

Tested: three runs of a real-window test (16 checks each, all passing): the ring opens over File Explorer; a click outside closes it; a click on the circle keeps it open; Escape closes it with focus and through the keyboard hook; pressing 1 and clicking a device send and close. The test also found that the ring window stayed fully transparent (so clicks passed through) until its first frame was drawn; the layer now catches clicks from the start and only the circle fades in. 319 tests, UI checks, CheckFormat and the Mac/Linux build pass.

MSIs in dist: hopo-Users-Setup-2.6.9-x64.msi (SHA-256 97847D35D5617170F3016B4A3773B6193A401404F22D026AC8A65656C0F9A3B2), hopo-Admin-Setup-2.6.9-x64.msi (SHA-256 257DB9AF6CBB0F9A899895CC6CF199861FC3EAB6835F016AB2270F2EF03D5D4C). The 2.6.7 MSIs were removed.
