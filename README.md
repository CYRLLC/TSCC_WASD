# YnyrWASD

A small Windows desktop tool that maps an XInput controller to a virtual
DualShock 4, with USB Nintendo Switch 2 Pro support. Built with C# / .NET 8 / WPF.
**MIT licensed · v0.2.0 preview** (SDL-derived protocol portions retain zlib licensing).

中文使用說明：[繁體中文](docs/README.zh-TW.md)

![Profile editor](docs/images/editor.png)

## What it does

- Reads the first connected XInput slot (0–3) and outputs one virtual PS4 controller.
- Also reads Nintendo Switch 2 Pro over USB; select its input mode in the editor.
  See [NS2 Pro setup and current limitations](docs/NS2-PRO.md).
- Maps face buttons, D-pad, shoulders, stick clicks, sticks and analog triggers.
- Provides editable profiles, dead zones, target polling frequency, import/export
  and manual start/stop.
- Releases held inputs on disconnect, cleans up virtual devices on stop/exit,
  and keeps malformed profile files intact.

**PS button icons depend on the game recognizing a DS4 and shipping PS artwork.**
This tool cannot force icons in games that only support Xbox input. It is an
early, narrowly scoped alternative inspired by reWASD, not feature parity.

Not implemented: arbitrary button remapping, macros, rumble forwarding,
keyboard/mouse input, DirectInput, touchpad, gyro, DualSense output, game detection,
automatic profile switching or overlays. Reserved JSON fields do not enable them.

## Requirements and setup

1. Windows 10/11 x64 with an XInput-compatible controller or USB NS2 Pro. Windows 11 is the local
   validation platform; other hardware/OS combinations need confirmation.
2. Install the latest serviced **.NET 8 Windows Desktop Runtime (x64)** from
   [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0).
3. Install ViGEmBus from the [official downloads](https://docs.nefarius.at/Downloads/).
   Reboot if requested. **ViGEmBus is end-of-life**; read the
   [upstream notice](https://docs.nefarius.at/projects/ViGEm/End-of-Life/).
   YnyrWASD does not bundle, silently install or update drivers.
4. Build below, or extract a maintainer-provided release ZIP. Run `YnyrWASD.App.exe`.
5. Select a profile and start mapping. Stop mapping before editing settings.
   Driver status is a registry hint; start attempts the actual device connection
   and reports errors if the driver is unavailable.

Only one application instance runs per Windows session. Changes apply to
the next mapping session; use **Save all** to retain them after exit. The UI is
currently Traditional Chinese.

### Avoid double input

Games may see both the physical and virtual controller. Optional HidHide can hide
the physical device. Follow the [official setup guide](https://docs.nefarius.at/projects/HidHide/Simple-Setup-Guide/):
allow `YnyrWASD.App.exe` in Applications, select only the physical controller in
Devices, then enable hiding. Do not hide the virtual DS4. Moving the app requires
updating its allowed path. Disable device hiding in HidHide to undo the setup.

Steam Input or another mapper may create additional virtual devices or change
what the game sees. Test with one mapper and review the game's controller settings.

## Build, test and package

Use Windows and .NET SDK 8.0.416 or a newer 8.0 feature band allowed by `global.json`.
Visual Studio 2022 with the .NET desktop workload is optional.

```powershell
dotnet restore YnyrWASD.sln --locked-mode
dotnet build YnyrWASD.sln -c Release --no-restore -warnaserror
dotnet test YnyrWASD.sln -c Release --no-build
dotnet run --project YnyrWASD.App
pwsh -File scripts/package.ps1
```

Packaging produces a framework-dependent Windows x64 ZIP and SHA-256 checksum
under `artifacts/`. The Desktop Runtime and drivers remain external prerequisites.
Update lock files intentionally when changing dependencies.
Tests cover input math, disconnect/error cleanup, profile persistence, native ABI
layout, XInput loading and WPF bindings/editor commands. They do not establish
real-controller/game compatibility. See [validation](docs/VALIDATION.md).

## Configuration and troubleshooting

Profiles live in `%APPDATA%\YnyrWASD\profiles.json`. Successful replacement saves
the previous file as `profiles.json.bak`. Import adds profiles with new IDs;
export writes the complete current list. Numeric legacy enum values remain readable.
See [profile format](docs/PROFILES.md) and [troubleshooting](docs/TROUBLESHOOTING.md).

Mapping uses no network, telemetry, background service or automatic updater.
The official download button opens a website in your browser. Settings stay local.

## Contributing and releases

See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md),
[release checklist](docs/RELEASING.md) and [roadmap](PLAN.md).
GitHub workflows test changes and can create a **draft preview release** from
a matching version tag. Hardware validation is required before claiming a stable release.

Licensed under [MIT](LICENSE). See [third-party notices](THIRD-PARTY-NOTICES.md).
