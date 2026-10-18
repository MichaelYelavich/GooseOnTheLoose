# Goose on the Loose — Beta Release 1

[Download the beta](https://github.com/MichaelYelavich/GooseOnTheLoose/releases/tag/v0.4.0-beta.1) · [Report a bug](https://github.com/MichaelYelavich/GooseOnTheLoose/issues/new/choose)

An unofficial Windows 11 sequel to Samperson's Desktop Goose. The goose lives directly on your desktop: wandering, honking, fetching sticks, stealing the cursor when provoked, and dragging back ordinary note and picture windows. Its procedural shape and planted footsteps follow the original game. Original sound samples are included.

## What changed in this beta

This is the next iteration after Desktop Goose 2 0.3.1, now called **Goose on the Loose**. It keeps the desktop geese, toys, water and native tools, and focuses on reliability and performance.

Fixed a crash when content disappears during a delivery, made picture preparation asynchronous, added safer note saving with conflict copies, hardened content reload and startup, and reduced rendering allocations. Tests now run separately from the game. Repeatable builds, GitHub checks and release tooling support future updates. Details: [release notes](RELEASE_NOTES.md).

## Install and play

Double-click **GooseOnTheLoose-Beta1-Setup.exe**. It installs per-user under `%LOCALAPPDATA%\Programs\GooseOnTheLoose` and creates a Start menu shortcut. Desktop and Windows-startup shortcuts are optional and start unchecked. Administrator access is not required. Updates preserve settings and content. Uninstall removes the application and shortcuts while keeping your Documents content.

For a portable copy, extract the entire Windows ZIP and double-click **GooseOnTheLoose.exe**. Keep every extracted file and folder alongside it, including Goose.Core.dll. Windows 11's included .NET Framework is sufficient; no account, network connection, or extra runtime download is required.

Right-click the goose tray icon for **Toy Box**, Play, Mischief, Pause, Quiet mode, Options, Your content, cleanup, and Quit. There is no required control window. Options includes a second goose, outfits, sizes, display selection, reduced motion, sound, and cursor-stealing preferences. Random cursor attacks start disabled; a provoked theft lasts at most 1.4 seconds.

## Toy Box and water

Open **Toy Box**, then drag an illustrated button onto the selected display. Place up to **16 independent objects**: balls, fetch sticks, peas, rubber ducks, ponds, puddles, and mud patches. Every launch starts with a clean desktop.

- Flick balls by dragging and releasing them. They bounce at desktop edges and geese nudge them.
- Sticks trigger fetch and are dropped on return, ready to move or throw again.
- Peas distract and calm the goose. Either goose can eat them.
- Rubber ducks can be nudged and drift on ponds.
- Geese float in ponds with ripples. Puddles wet their feet without swimming; mud patches make them muddy. Clean water washes mud off.
- Each goose leaves its own wet or muddy footprints after leaving a surface: 20 foot landings by default, fading over 10 seconds, with at most 256 prints shared between geese.

Toys are draggable. Water and the goose background let desktop input pass through. While the Toy Box is open, water gets move and resize handles; every object gets a small remove button. Close the box to hide handles. **Clear desktop objects** removes toys, water, and footprints without quitting. Pause stops gameplay; placement tools remain available.

## Leave the desktop

Press **Escape** briefly to release the cursor immediately. Hold it for **five seconds** to dismiss the geese: a countdown appears, then all active geese burst together into cartoon feathers and smoke with a soft pop. Releasing Escape early cancels the countdown. Reduced motion uses a short fade. Everything disappears within one second after the countdown, including delivered windows, objects, overlays, and the tray icon.

Tray **Quit**, **Ctrl+Alt+Q**, installer shutdown, and Windows shutdown clean up immediately.

| Shortcut | Action |
| --- | --- |
| Ctrl+Alt+G | Call the goose |
| Ctrl+Alt+F | Drop peas |
| Ctrl+Alt+B | Throw a ball |
| Ctrl+Alt+T | Throw a fetch stick |
| Ctrl+Alt+N | Bring a note |
| Ctrl+Alt+I | Bring a picture |
| Ctrl+Alt+H | Honk |
| Ctrl+Alt+P | Pause / resume |
| Ctrl+Alt+K | Quiet mode |
| Ctrl+Alt+Q | Quit immediately |

Closing a delivered window can provoke the goose. Cleanup and shutdown never provoke retaliation. At most four delivered windows remain open.

## Your content

The tray's **Your content** menu includes **Copy diagnostics** and offers **Import Pictures**, **Edit Notes**, **Open Content Folder**, **Import Legacy Content Folder**, and **Reload content**.

Windows' Documents known-folder location is used. Existing DesktopGoose2 collections remain in their original folder; fresh profiles use:

```text
Documents/GooseOnTheLoose/
  settings.json
  Pictures/
  Notes/
  Behaviors/default.json
  README.txt
```

Import multiple PNG, JPEG, or GIF files at once, or copy them into Pictures. Each picture may be up to 8 MB and 8192 pixels per side. Animations are limited to 500 frames and 128 million decoded pixels. Notes are ordinary .txt files of up to 1000 characters; the small native editor creates them and saves changed text after a one-second pause. External edits produce a conflict copy instead of being overwritten. Files reload after a 650 ms save debounce. Incomplete pictures are skipped. Invalid behavior or settings files keep the last valid configuration.

The behavior preset controls prank intervals, activity weights, toy approach/nudge speeds, trail length, and footprint lifetime. Its `_comment` field and README.txt explain editing; keep the file valid JSON. Scripted mods and original DLL mods are outside this version. Sound samples can be replaced in Assets/Sounds.

Existing `%LOCALAPPDATA%\DesktopGoose2\settings.json` is copied once if Documents settings do not exist. **Import Legacy Content Folder** copies older pictures, text files, or JSON note packs without overwriting current files. Sample content is seeded only when missing; removed samples are not recreated on each launch. Objects, footprints, and delivered windows are never saved.

## Build and verify

Developers need PowerShell 7 and the exact .NET SDK in `global.json`. Players need no SDK. See [CONTRIBUTING.md](CONTRIBUTING.md) for the full workflow.

```powershell
./build.ps1 -Locked
./tools/verify.ps1 -Interactive -Format
```

Builds go to `dist`, and reports/staging go to `.work`. The shipped game contains no test runner. `tools/package.ps1` produces an installer, portable ZIP and committed-source ZIP; `tools/verify-installer.ps1` verifies a real 0.3.1 upgrade with isolated product/data paths. Inno Setup 6.7.3 is discovered through `INNO_COMPILER` or installed with `tools/install-inno.ps1`.

Commands for the running instance: `--come`, `--peas`, `--ball`, `--fetch`, `--note`, `--image`, `--honk`, `--pause`, `--quiet`, `--menu`, `--quit`, `--toolbox`, `--content-folder`, `--reload`, `--clear`, and `--diagnostics`. A second launch calls the existing goose. `--quit` waits for cleanup. `--seed-content` initializes missing content without gameplay. `--snapshot output.png` exports goose artwork.

**Your content > Copy diagnostics** copies a summary without custom content or paths. When no game is running, `--diagnostics` writes that summary to diagnostics.txt in the content folder. Error logs stay local and rotate across three files. There are no automatic uploads. runtime.json is enabled only for tests or with `GOOSE_DIAGNOSTICS=1`.

For isolated tests, `DESKTOPGOOSE2_DATA_DIR` overrides the data directory and gives the instance separate command events and a separate single-instance lock. Normal launches use Documents. Old and renamed executable names share one instance after an upgrade.

Beta limitations: unsigned installer; one selected display at a time; mixed-DPI/display hot-plugging, real Documents redirection, and startup after reboot require further hardware/environment testing. See [validation](VALIDATION.md) and the [roadmap](docs/ROADMAP.md).

Research: [RESEARCH.md](RESEARCH.md). Test evidence and limits: [VALIDATION.md](VALIDATION.md). Sound sources: [Assets/Sounds/PROVENANCE.md](Assets/Sounds/PROVENANCE.md).

Inspired by [Desktop Goose by Samperson](https://samperson.itch.io/desktop-goose). This is an independent fan sequel.
