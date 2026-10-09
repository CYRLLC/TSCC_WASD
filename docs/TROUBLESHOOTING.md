# Troubleshooting

| Symptom | Check |
| --- | --- |
| App does not launch | Install .NET 10 **Windows Desktop** Runtime x64; extract the whole ZIP |
| Windows SmartScreen warns about the EXE | Preview builds are unsigned (see [SIGNING.md](SIGNING.md)); choose "More info → Run anyway" only for a ZIP whose SHA-256 matches the release |
| Nothing appears after sign-in | With "Start with Windows" the app runs in the notification area; open it from the tray icon |
| No rumble | Check "Forward game rumble"; the game must send rumble to the DS4; NS2 Pro rumble needs USB |
| Driver connection fails | Install official ViGEmBus, reboot if requested; registry status alone does not prove the driver is running |
| Waiting for controller | Confirm the controller is connected (NS2 Pro over USB); if hiding was configured manually, check HidHide's allowed executable path |
| Duplicate presses / prompts flicker Xbox↔PS | Install HidHide and keep "hide physical controllers while mapping" on; start mapping before the game; restart Steam if it started first (TSCC_WASD offers this); stop other mappers |
| Still seeing Xbox icons | Confirm native DS4/game artwork support and review Steam Input settings |
| Settings fail to load | Preserve broken JSON, restore `.bak` or fix validation errors, then reload |
| Settings lost after exit | Save all before closing; edits and imports are not auto-saved |
| Wrong controller selected | Auto-detect follows the last controller pressed; XInput uses the first available slot |
| Mapping stops unexpectedly | Mapping retries on its own; copy the visible error and record reproduction steps for an issue |
| Controller invisible to games after a crash | Relaunch TSCC_WASD once; it restores the HidHide settings it changed |

To remove TSCC_WASD, close it and delete its extracted directory. Settings remain
in `%APPDATA%\TSCC_WASD`; remove that folder only to discard them.
Drivers are separate installations. Undo HidHide configuration before removing
its application allowlist entry, and use upstream instructions to uninstall drivers.
