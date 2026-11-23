# hopo Windows: stop the lag, fix the taskbar icon

Parts 1 and 2 of the [2.6 plan](2026-11-24-hopo-2.6-plan.md). The Android app is paused at the user's request.

Lag (found in the code):
- [x] Home's rings redraw on a 30 frames-a-second animation clock instead of every screen refresh (`CompositionTarget.Rendering` kept the whole window repainting at 60–144 Hz), and stop while the window is minimised, hidden in the tray or Home is closed.
- [x] Endless animations (ripples, device blinks and pings, glass edge sweep, background glows) run at 30 frames a second, and Home stops its own when it closes or redraws a device. Before, every redraw left the old device animations running in the background, so they piled up during a transfer.
- [x] Home redraws the devices and the approval card only when they change, not on every transfer progress tick.
- [x] Page refreshes from transfers, devices, phones, sync and notes are batched to at most four a second.
- [x] The large blurred shadows (dock, panels, approval card) become layered soft shadows, and the device and beacon glows become gradients; a blur is redrawn every frame anything moves under it.

Taskbar icon:
- [x] Set the app's taskbar identity for the whole process at start (hopo.LocalFileSharing / hopo.Admin), the same one the window uses.
- [x] Give the installer's Start menu and desktop shortcuts that identity too, so Windows ties the window, the shortcut and its icon together.
- [x] Add 20, 24, 40 and 64 px images to the icon (24 px is the taskbar size at 100% scale). The 16, 32 and 48 px images were stored as PNG, which some Windows icon readers draw as noise; all sizes below 256 px are now plain bitmaps.

Checked: Test (306 pass), CheckFormat, Build, and the UI screenshot checks (29 pass; the incoming-notes check now waits for the batched refresh). Home looks the same with the new shadows and glows.
Not measured: CPU before and after (the screenshot checks run with motion off). The installer shortcut change is checked when the next MSI is built.
