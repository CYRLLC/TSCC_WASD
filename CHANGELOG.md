# Changelog

## 0.3.0 — preview (unreleased)

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
