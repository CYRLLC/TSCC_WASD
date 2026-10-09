# Validation record

## Local automated validation — 2026-09-08

Environment: Windows 11 x64 (build 26100), .NET SDK 8.0.416.
Release build: zero warnings/errors. Automated tests: **56 passed, 0 failed**.
Release build and driver-independent tests are exercised locally; GitHub-hosted
workflows need their first run after the repository is pushed.

Coverage includes all signed-short axis values, endpoints/inversion/dead zones,
malformed/empty/legacy profiles, backups, invalid saves, isolated session settings,
disconnect neutralization, driver errors, callback failures and disposal.
WPF editor commands/bindings are exercised on an STA thread and rendered for review.
The inbox XInput API is loaded; this alone does not prove device/game compatibility.

NuGet transitive audit after removing SharpDX reports no known vulnerable packages
from the configured NuGet source. This is a point-in-time package audit, not a
security assessment of Windows or the external ViGEmBus driver.

Optional real-driver smoke check also passed against the packaged DLLs:
virtual DS4 connection, neutral report submission, disconnection and disposal.
Run it explicitly on a machine with ViGEmBus installed:

```powershell
pwsh -File scripts/smoke-driver.ps1 -AppDirectory <extracted-app-directory>
```

This creates a short-lived neutral virtual controller. It does not test physical
button input, HidHide setup, game recognition or long-running stability.

## Manual hardware validation

### NS2 Pro USB — 0.2.0

Steam remained running at the user's request. Shared HID mode received valid
64-byte report 0x05 packets from the attached NS2 Pro and ran for 30 seconds,
sampling 1,916 valid states while submitting them to a temporary virtual DS4.
No button presses were observed during that automated capture. In a subsequent
manual test on 2026-09-08, the user confirmed live input in YnyrWASD and successful
input through the virtual PS4 in Steam's device test. **The basic NS2 Pro USB →
shared HID → virtual DS4 path is user-confirmed working.** This does not certify
every button, full stick travel or in-game compatibility.
USB calibration access returned access denied while Steam held the interface;
the fallback used nominal calibration.
Standalone initialization was subsequently tested below. Physical hot-plug remains pending.

### NS2 Pro diagnosis — 2026-09-11

Tested the running app's packaged 0.2.0 DLLs, with the WPF mapping stopped.
HidHide was initially running with cloaking OFF, no hidden devices, and only its
CLI allowlisted. DS4Windows was also running; its log showed controller search,
but no evidence of an additional mapping was observed during these tests.
The saved YnyrWASD profile used XInput (`inputType: 0`), not NS2 Pro USB.

| Configuration | Duration | Fresh-state polls | Missing polls after first input |
| --- | --- | --- | --- |
| Steam running, hiding off, input only | 30 s | 1,918 / 1,918 | 0 |
| Steam normally exited, hiding off, input only | 15 s | 958 / 959 | 0 |
| Steam exited, NS2 Pro hidden, diagnostic process allowlisted, virtual DS4 output | 30 s | 1,915 / 1,915 | 0 |

Steam-running input used shared HID/nominal calibration. Both Steam-exited runs
successfully performed USB initialization and loaded factory/user calibration.
The initial unavailable poll in the second run occurred before first input.
These counts are polling observations, not unique USB packets. The reader regards
reports as fresh for 250 ms; this test does not measure sub-250 ms packet gaps.

A separate Windows PowerShell process, absent from the HidHide allowlist, could
open the physical HID before hiding. During hiding it received access denied
(Win32 error 5) for the physical HID but could open the virtual DS4 HID. This
verifies device isolation and virtual-device accessibility, not game behavior.
The mapping run submitted states without reported errors; no button presses
were observed, so individual controls and end-to-end input remain unverified.

The physical controller was not unplugged between runs: standalone cold-plug
initialization is still unverified. The reported game is Zenless Zone Zero;
the game issue was not reproduced in this session. Temporary HidHide changes
were reverted and Steam was relaunched after the comparison.

### Other devices and scenarios

| Check | Result |
| --- | --- |
| Xbox USB controller: all buttons/sticks/triggers | Pending |
| Xbox Bluetooth controller | Pending |
| Third-party XInput controller | Pending |
| Hot-unplug while holding a button; reconnect | Pending |
| 50 start/stop cycles with virtual device enumeration | Pending |
| Close app during active mapping | Pending |
| HidHide allowlist and physical/virtual access isolation | Passed, 2026-09-11; in-game double-input prevention pending |
| Native DS4-compatible game's PS icons | Pending |
| Steam device test: NS2 Pro shared HID → virtual PS4 | Passed, user-confirmed; game interaction pending |
| Windows 10 clean machine | Pending |

Record controller model, transport, OS, driver versions, game version and observed
result for each test. Automated fake-device tests cannot replace these checks.
