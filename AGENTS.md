# GoTweaks Lite

This fork targets Lenovo Legion Go 2 on Windows 11. Keep Rulob's `main` as
`upstream/main` and use `origin` for the personal fork.

- `XboxGamingBar` is the UWP Game Bar widget. `XboxGamingBarHelper` is the
  elevated .NET Framework desktop helper. `Shared` holds IPC and profile
  models used by both. `XboxGamingBarPackage` packages both into one MSIX.
- Extend the existing game detection and profile flow in the helper. Keep
  controller and scaling changes separate from the Lenovo WMI TDP, PID AutoTDP,
  fan, RGB, OSD and controller-mode paths.
- Run `dotnet run --project Tests/Shared.Tests` for hardware-independent
  regression tests. The test project is separate from the solution.
- See [Windows build setup](docs/windows-build.md) for the x64 baseline build
  and [restoration notes](docs/feature-baseline.md) for historical references.
  Record Game Bar, controller and Xbox FSE results separately on the Legion Go 2.
