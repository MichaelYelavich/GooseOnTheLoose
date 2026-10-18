# Contributing

Install Git, PowerShell 7, and the exact .NET SDK in `global.json`. Players need neither the SDK nor a GitHub account. The game targets .NET Framework 4.8 on Windows 11.

```powershell
./build.ps1 -Locked
./tools/verify.ps1 -Interactive -Format
```

`GooseOnTheLoose.slnx` contains the core simulation/configuration, Windows application, console regression runner, and build-only asset generator. Tests use isolated content and offscreen windows. Interactive checks open and cancel temporary Windows dialogs. Run them on an unlocked desktop.

Use `dotnet format GooseOnTheLoose.slnx whitespace --no-restore` before committing. Warnings are errors; targeted reliability analyzers are configured in `.editorconfig`. Restore uses committed package lock files. Change dependency versions deliberately and review lock-file changes.

For installer work, run `./tools/install-inno.ps1`, or set `INNO_COMPILER` to Inno Setup 6.7.3's `ISCC.exe`. Then commit the intended source, run `./tools/package.ps1`, `./tools/verify-installer.ps1`, and `./tools/verify-package.ps1`. Installer upgrade tests need Git history containing the 0.3.1 baseline; an extracted source ZIP can build and run application checks without that history.

Keep mechanical cleanup separate from behavior fixes. Add regressions for reproducible failures. Do not add tests or fixtures to the shipped application. Preserve content formats, command names, IPC identity, and desktop click-through behavior.

Build outputs belong in `dist`, reports/staging in `.work`, and only the newest three distributables in `artifacts`. None are committed. Package source comes from the selected Git commit, so commit before creating final packages. Do not commit custom user content or diagnostic logs.

See [architecture](docs/ARCHITECTURE.md), [release checklist](docs/RELEASING.md), and [roadmap](docs/ROADMAP.md).
