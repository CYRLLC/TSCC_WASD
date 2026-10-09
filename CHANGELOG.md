# Changelog

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
