# Changelog

## 0.4.0 — preview

- Upgraded to .NET 10 (LTS); .NET 8 support ends in November 2026. Updated test packages and CI actions.
- Rumble forwarding from games to Xbox (XInput) and NS2 Pro (HD rumble), with a per-profile switch.
- PS button from Xbox Guide / NS2 Home; touchpad click from NS2 Capture.
- NS2 Pro gyro and accelerometer forwarded as DualShock 4 motion.
- The virtual DS4 now receives full raw reports (needed for PS button, touchpad and motion).
- Wired Xbox controllers (XUSB/GIP) are hidden too; ViGEm virtual pads are excluded.
- Start with Windows (per-user Run entry), notification-area icon, automatic mapping, and
  automatic Steam launch/restart after hiding, so Steam never needs a manual restart.
- Bilingual UI (English / Traditional Chinese), following Windows or chosen in settings.
- Opening the app again brings the running instance forward.
- The post-stop "restart Steam?" prompt now covers any Steam that started while controllers were hidden.
- New app icon; code-signing plan documented in docs/SIGNING.md.

## 0.3.0 — preview

- Auto-detect input (new default): follows whichever of NS2 Pro USB / XInput is being used.
- Automatic HidHide isolation while mapping, with exact restore on stop and crash recovery on launch.
  Fixes button prompts flickering between Xbox and PS when the game also saw the physical controller.
- Detects Steam started before hiding (Steam Input would still forward the physical controller)
  and offers to restart Steam so it only sees the virtual DS4. If it did, stopping or
  closing asks whether to restart Steam again so it sees the physical controller.
- Mapping no longer ends on a transient input or virtual-driver error; the virtual DS4 reconnects.
- Fixed a crash when closing the main window after cleanup completed synchronously.

## 0.2.0 — preview (unreleased)

- Added Nintendo Switch 2 Pro USB input selection and HID report decoding.
- Shared HID input when Steam owns the control interface, with explicit nominal calibration status.
- Native WinUSB initialization and calibration reads when the interface is available (hardware validation pending).
- Live button/stick/trigger diagnostics, stale-report neutralization and reconnect handling.
- Added parser/calibration regression tests and optional NS2 hardware smoke script.
- Preserved the SDL protocol adaptation's zlib attribution.

## 0.1.0 — preview (unreleased)

- Basic Windows XInput to virtual DualShock 4 mapping and WPF profile editor.
- Profile create/remove/save/import/export, dead-zone and polling controls.
- Native Windows XInput access; removed SharpDX and its obsolete dependencies.
- Neutral output on disconnect; reliable stop/error/shutdown cleanup.
- Safe signed-axis conversion and validated, snapshotted settings.
- Atomic profile replacement with backup; malformed files are preserved.
- Explicit rejection of unsupported outputs; truthful feature descriptions.
- Official driver download guidance instead of elevated silent installation.
- Single instance per Windows session, regression tests, CI and packaging.
- MIT license, third-party notices, contributor and release documentation.
