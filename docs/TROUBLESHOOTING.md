# Troubleshooting

| Symptom | Check |
| --- | --- |
| App does not launch | Install .NET 8 **Windows Desktop** Runtime x64; extract the whole ZIP |
| Driver connection fails | Install official ViGEmBus, reboot if requested; registry status alone does not prove the driver is running |
| Waiting for controller | Confirm the controller is connected (NS2 Pro over USB); if hiding was configured manually, check HidHide's allowed executable path |
| Duplicate presses / prompts flicker Xbox↔PS | Install HidHide and keep "hide physical controllers while mapping" on; start mapping before the game; stop other mappers |
| Still seeing Xbox icons | Confirm native DS4/game artwork support and review Steam Input settings |
| Settings fail to load | Preserve broken JSON, restore `.bak` or fix validation errors, then reload |
| Settings lost after exit | Save all before closing; edits and imports are not auto-saved |
| Wrong controller selected | Auto-detect follows the last controller pressed; XInput uses the first available slot |
| Mapping stops unexpectedly | Mapping retries on its own; copy the visible error and record reproduction steps for an issue |
| Controller invisible to games after a crash | Relaunch YnyrWASD once; it restores the HidHide settings it changed |

To remove YnyrWASD, close it and delete its extracted directory. Settings remain
in `%APPDATA%\YnyrWASD`; remove that folder only to discard them.
Drivers are separate installations. Undo HidHide configuration before removing
its application allowlist entry, and use upstream instructions to uninstall drivers.
