# Desktop Goose 2 · 0.3.1

An unofficial Windows 11 sequel to Samperson's Desktop Goose. The goose lives directly on your desktop: wandering, honking, fetching sticks, stealing the cursor when provoked, and dragging back ordinary note and picture windows. Its procedural shape and planted footsteps follow the original game. Original sound samples are included.

## What changed in 0.3.1

The companion now looks like a Canada goose, and both geese wear the selected scarf or sunhat. Fixed a crash when importing pictures after closing Toy Box, a stride-speed change that could reverse a foot mid-step, and made footprint rendering independent of moving overlay crops. Footprints retain their original size when goose size changes.

Picture imports validate every frame before publishing a complete file, run without freezing the desktop, support cancellation, and summarize skipped files once. Gameplay pauses during modal content tools and then returns to its previous state. The bundled honk picture is text-only; upgrades replace exact original copies while preserving custom art. Water handles and the dismissal countdown stay inside the selected display. Details: [RELEASE_NOTES.md](RELEASE_NOTES.md).

## Install and play

Double-click **DesktopGoose2-Setup.exe**. It installs per-user under `%LOCALAPPDATA%\Programs\DesktopGoose2` and creates a Start menu shortcut. Desktop and Windows-startup shortcuts are optional and start unchecked. Administrator access is not required. Updates preserve settings and content. Uninstall removes the application and shortcuts while keeping your Documents content.

For a portable copy, extract the entire Windows ZIP and double-click **DesktopGoose2.exe**. Keep Assets and Packs alongside it. Windows 11's included .NET Framework is sufficient; no account, network connection, or extra runtime download is required.

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

The tray's **Your content** menu offers **Import Pictures**, **Edit Notes**, **Open Content Folder**, **Import Legacy Content Folder**, and **Reload content**.

Windows' Documents known-folder location is used, including redirected Documents folders:

```text
Documents/DesktopGoose2/
  settings.json
  Pictures/
  Notes/
  Behaviors/default.json
  README.txt
```

Import multiple PNG, JPEG, or GIF files at once, or copy them into Pictures. Each picture may be up to 8 MB and 8192 pixels per side. Animations are limited to 500 frames and 128 million decoded pixels. Notes are ordinary .txt files of up to 1000 characters; the small native editor creates and saves them. Files reload after a 650 ms save debounce. Incomplete pictures are skipped. Invalid behavior or settings files keep the last valid configuration.

The behavior preset controls prank intervals, activity weights, toy approach/nudge speeds, trail length, and footprint lifetime. Its `_comment` field and README.txt explain editing; keep the file valid JSON. Scripted mods and original DLL mods are outside this version. Sound samples can be replaced in Assets/Sounds.

Existing `%LOCALAPPDATA%\DesktopGoose2\settings.json` is copied once if Documents settings do not exist. **Import Legacy Content Folder** copies older pictures, text files, or JSON note packs without overwriting current files. Sample content is seeded only when missing; removed samples are not recreated on each launch. Objects, footprints, and delivered windows are never saved.

## Build and verify

Quit the goose, then run `powershell -NoProfile -File .\build.ps1` from the source package. Windows' bundled Framework compiler produces dist/DesktopGoose2.exe with no external runtime dependencies.

`tools/verify.ps1` builds and runs simulation, content, shutdown, input-controller, and native rendering checks. Reports are under artifacts. `tools/package.ps1 -Compiler "path\to\ISCC.exe"` builds the installer with Inno Setup 6.7+ and creates portable/source ZIPs. [Inno Setup per-user installation documentation](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm).

`tools/verify-installer.ps1` tests a separate installer identity and isolated application/data folders on the current Windows host. It creates its own named test shortcuts and removes them during uninstall. It does not provide a disposable OS or change the real Documents known folder. It expects ISCC.exe under artifacts/build-tools/Inno Setup.

Commands for the running instance: `--come`, `--peas`, `--ball`, `--fetch`, `--note`, `--image`, `--honk`, `--pause`, `--quiet`, `--menu`, `--quit`, `--toolbox`, `--content-folder`, `--reload`, and `--clear`. A second launch calls the existing goose. `--quit` waits for cleanup. `--seed-content` initializes missing content without gameplay. `--snapshot output.png` exports goose artwork. runtime.json contains local diagnostics.

For isolated tests, `DESKTOPGOOSE2_DATA_DIR` overrides the data directory and gives the instance separate command events and a separate single-instance lock. Normal launches use Documents.

Research: [RESEARCH.md](RESEARCH.md). Test evidence and limits: [VALIDATION.md](VALIDATION.md). Sound sources: [Assets/Sounds/PROVENANCE.md](Assets/Sounds/PROVENANCE.md).

Inspired by [Desktop Goose by Samperson](https://samperson.itch.io/desktop-goose). This is an independent fan sequel.
