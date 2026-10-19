# Contributing to Goose on the Loose

Goose on the Loose is an independent spiritual successor to Desktop Goose. Contributions should make the geese more enjoyable to have on the desktop, with clear code and reliable behavior underneath.

[← README](README.md) · [Architecture](docs/ARCHITECTURE.md) · [Roadmap](docs/ROADMAP.md) · [Release checklist](docs/RELEASING.md)

## Development requirements

| Dependency | Required for |
| --- | --- |
| **Windows 11** | Building and checking the native application |
| **Git** | Cloning, pulling, committing and pushing source; source packaging and upgrade-history checks |
| **PowerShell 7** | Build, verification and packaging scripts |
| **.NET SDK pinned in [global.json](global.json)** | Building all projects; the exact version is required because SDK roll-forward is disabled |
| **Inno Setup 6.7.3** | Installer packaging and installer checks only |

The application targets **.NET Framework 4.8**. Players need no SDK or GitHub account. Build-time reference assemblies are restored through the locked **Microsoft.NETFramework.ReferenceAssemblies 1.0.3** dependency; each project's **packages.lock.json** is committed. Initial setup and package restore need access to the dependency sources.

## Build and run

Open PowerShell 7 in a local clone:

```powershell
git clone https://github.com/MichaelYelavich/GooseOnTheLoose.git
cd GooseOnTheLoose
./build.ps1 -Locked
./dist/GooseOnTheLoose.exe
```

Build output goes to **dist**. The solution, **GooseOnTheLoose.slnx**, separates four responsibilities:

| Project | Responsibility |
| --- | --- |
| **Goose.Core** | Shared simulation and configuration |
| **GooseOnTheLoose** | Native Windows application, rendering and desktop tools |
| **Goose.Tests** | Console regression runner and native checks |
| **Goose.AssetGenerator** | Build-time asset generation |

Tests and their fixtures are excluded from the shipped executable. See [architecture](docs/ARCHITECTURE.md) for runtime ownership, content preparation and shutdown.

## Verify a change

```powershell
./tools/verify.ps1 -Interactive -Format
```

This rebuilds with locked dependencies, checks formatting and runs regressions plus native checks. Run it on an **unlocked Windows desktop**: interactive checks open and cancel temporary Windows dialogs. Tests use isolated content and offscreen windows.

For a session without an interactive desktop:

```powershell
./tools/verify.ps1 -Format
```

The native modal-dialog checks are then explicitly reported as skipped. Hosted checks cannot replace the local desktop and hardware release gates in [RELEASING.md](docs/RELEASING.md).

To apply whitespace formatting before committing:

```powershell
dotnet format GooseOnTheLoose.slnx whitespace --no-restore
```

Warnings are errors, and targeted reliability analyzers are configured in **.editorconfig**. Update dependency versions deliberately and review the resulting lock-file changes.

## Prepare a source contribution

Use a focused branch and describe the change in terms of what a player or developer will notice:

```powershell
git switch -c codex/describe-your-change
# Make and verify the change, then commit it.
git push -u origin codex/describe-your-change
```

Open a pull request against **main** with the problem, resulting behavior and checks performed. Keep mechanical cleanup separate from behavior fixes. Add regression coverage for reproducible failures, and preserve content formats, command names, IPC identity and desktop click-through behavior.

**Source updates and releases are separate.** Ordinary pushes and pull requests run the Windows validation workflow. They do not publish a downloadable release or move an existing release tag. Publishing uses the separate, manually triggered release workflow after the release checklist is complete.

## Package locally

Installer work needs Inno Setup. Install the pinned compiler with:

```powershell
./tools/install-inno.ps1
```

Alternatively, set **INNO_COMPILER** to Inno Setup 6.7.3's **ISCC.exe**. The packaging scripts also discover standard installation locations.

Commit the intended source first, then run:

```powershell
./tools/package.ps1
./tools/verify-installer.ps1
./tools/verify-package.ps1
```

The source ZIP is made from the selected Git commit, so packaging requires a clean working tree. Installer regression checks need Git history containing the 0.3.1 baseline. An extracted source ZIP can build and run application checks without that history.

## Keep the repository clean

| Location | Contents |
| --- | --- |
| **dist/** | Local application build |
| **.work/** | Temporary files, reports, staging and release candidates |
| **artifacts/** | Only the newest verified installer, portable ZIP and source ZIP |

These outputs are ignored by Git. Do not commit custom user content, diagnostic logs, downloaded research repositories or generated binaries. See [RESEARCH.md](RESEARCH.md) for the useful research references without those checkouts.

For an isolated development instance, set **DESKTOPGOOSE2_DATA_DIR** to a temporary content folder. That also separates its single-instance lock and command events from normal play. Full command and diagnostics details are in the [player guide](docs/PLAYING.md#command-line-reference).
