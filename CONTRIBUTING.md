# Contributing

Issues and PRs are welcome in English or Traditional Chinese.
Keep changes focused and describe observable behavior and validation.

1. Fork and clone the repository; create a topic branch.
2. Install the Windows/.NET prerequisites in README.
3. Run locked restore, Release build and `dotnet test YnyrWASD.sln -c Release`.
4. Add regression coverage for lifecycle, mapping or persistence changes.
   Driver-free fakes belong in tests; hardware checks go in `docs/VALIDATION.md`.
5. Update documentation and CHANGELOG for user-visible changes. User-facing text is
   bilingual: write both versions with `L.T("中文", "English")` in C# or
   `{app:T Zh=..., En=...}` in XAML.
6. Submit a PR explaining the problem, final behavior, tests and limitations.

Do not commit bin/obj, personal profiles, credentials, signing keys or private logs.
Regenerate lock files intentionally and run the transitive vulnerability check
when updating packages. Drivers/runtimes must not be silently bundled.

Architecture: `Core` owns profiles, settings, input polling, axis math, HidHide/Steam
handling and virtual output; `App` owns WPF presentation, the tray icon and commands; `Tests` uses fake devices and WPF on an STA
thread. The coordinator serializes start/stop/disposal.

Contributions use this repository's MIT license. Be respectful, discuss technical
points in good faith, and avoid harassment or personal attacks.
