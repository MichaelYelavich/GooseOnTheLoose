# Playing Goose on the Loose

An independent spiritual successor to [Desktop Goose by Samperson](https://samperson.itch.io/desktop-goose), with geese that live directly on your Windows 11 desktop.

[← Back to the README](../README.md) · [Get Beta Release 1](https://github.com/MichaelYelavich/GooseOnTheLoose/releases/tag/v0.4.0-beta.1)

## Install and play

**Installer:** double-click **GooseOnTheLoose-Beta1-Setup.exe**. It installs for your Windows account and creates a Start menu shortcut. Administrator access is not required. Desktop and Windows-startup shortcuts are optional and start unchecked.

Fresh installations use `%LOCALAPPDATA%\Programs\GooseOnTheLoose`. Upgrades retain the existing installation directory, preferences and custom content. Uninstall removes the application and shortcuts while keeping your Documents content.

**Portable:** extract the entire Windows ZIP and run **GooseOnTheLoose.exe**. Keep all extracted files and folders together, including **Goose.Core.dll**, the executable's configuration file, **Assets** and **Packs**.

Windows 11's included .NET Framework is sufficient. No account, network connection or extra runtime download is required. The installer is unsigned; download it from the project's [GitHub release page](https://github.com/MichaelYelavich/GooseOnTheLoose/releases/tag/v0.4.0-beta.1).

## The tray menu

Right-click the goose tray icon to open the controls. There is no required control window.

| Control | What it does |
| --- | --- |
| **Toy Box** | Opens the optional floating palette for placing toys and water. |
| **Play / Mischief** | Calls the goose, offers toys or asks for a note, picture or honk. |
| **Pause / Quiet mode** | Stops gameplay or quiets the goose. Placement tools remain available while paused. |
| **Options** | Changes the companion, shared outfits, size, selected display, reduced motion, sound and cursor preferences. |
| **Your content** | Imports pictures, edits notes, opens the content folder, reloads content or copies diagnostics. |
| **Cleanup / Quit** | Clears objects, closes delivered windows or exits completely. |

The optional companion is a Canada goose. Both geese use the same appearance preference, including **None**, **Scarf** and **Sunhat**; the companion remains a little smaller.

Random cursor attacks start disabled. A provoked cursor theft lasts at most 1.4 seconds, and a short **Escape** press releases the cursor immediately.

## Toys and water

Open **Toy Box**, then drag an illustrated button onto the selected display. Place up to **16 independent objects** in any combination.

| Item | Try this |
| --- | --- |
| **Ball** | Drag and release to flick it. Balls bounce at desktop edges, and geese nudge them. |
| **Fetch stick** | Put one out for the goose to retrieve. It drops the stick on return, ready to move or throw again. |
| **Peas** | Distract and calm a goose. Either goose can eat them. |
| **Rubber duck** | Nudge it on land or let it drift in a pond. |
| **Pond** | Watch geese float with ripples. Water washes mud off their feet. |
| **Puddle** | Wet their feet without making them swim. |
| **Mud patch** | Give them muddy feet and a trail to leave behind. |

Each goose tracks its own wet or muddy feet. After leaving a surface, it leaves prints for **20 foot landings** by default. Prints fade over **10 seconds**, with a shared maximum of **256**.

Toys are draggable. Geese and water let desktop clicks pass through. While the Toy Box is open, water gains move and resize handles, and every object gains a small remove button. Close the box to hide the handles.

**Clear desktop objects** removes toys, water and footprints without quitting. Every launch starts with a clean desktop; placed objects and prints are never saved.

## Notes, pictures and shortcuts

Geese can drag small native note and picture windows onto the desktop. Closing one can provoke a goose; the cleanup commands and shutdown never provoke retaliation. At most **four delivered windows** remain open.

| Shortcut | Action |
| --- | --- |
| **Ctrl+Alt+G** | Call the goose |
| **Ctrl+Alt+F** | Drop peas |
| **Ctrl+Alt+B** | Throw a ball |
| **Ctrl+Alt+T** | Throw a fetch stick |
| **Ctrl+Alt+N** | Bring a note |
| **Ctrl+Alt+I** | Bring a picture |
| **Ctrl+Alt+H** | Honk |
| **Ctrl+Alt+P** | Pause / resume |
| **Ctrl+Alt+K** | Quiet mode |
| **Ctrl+Alt+Q** | Quit immediately |

## Dismiss the geese

- **Tap Escape:** release the cursor immediately.
- **Hold Escape for five seconds:** a countdown appears, then every active goose bursts into cartoon feathers and smoke with a soft pop when sound is enabled. Release early to cancel. Reduced motion uses a short fade.
- **Tray Quit or Ctrl+Alt+Q:** exit immediately.

After the five-second countdown, everything disappears within one second: geese, delivered windows, toys, water, footprints, overlays and tray icon. Installer shutdown and Windows shutdown also clean up immediately. Preferences and your custom files remain.

## Your content

Use **Your content → Import Pictures**, **Edit Notes** or **Open Content Folder** in the tray. Files reload while the game runs.

The application uses Windows' Documents known-folder location, including redirected Documents folders. A fresh profile creates:

```text
Documents/GooseOnTheLoose/
  settings.json
  Pictures/
  Notes/
  Behaviors/default.json
  README.txt
```

Existing **Documents/DesktopGoose2** collections continue to work in place. If both collections exist, an initialized **GooseOnTheLoose** folder takes priority; the application does not automatically merge or move collections.

### Pictures and notes

Import multiple **PNG, JPEG or GIF** files at once, or copy them into **Pictures**. Each picture may be up to **8 MB** and **8192 pixels per side**. Animated files are limited to **500 frames** and **128 million decoded pixels**. Invalid or incomplete pictures are skipped.

Notes are ordinary **.txt** files of up to **1000 characters**. The native note editor creates and edits them, saving changed text after a one-second pause. If a file changes externally during editing, a conflict copy preserves the edit instead of overwriting the external change.

Folder changes reload after a **650 ms** save debounce. Invalid settings or behavior files leave the last valid configuration in use.

### Behavior presets and sound

**Behaviors/default.json** controls prank intervals, activity weights, toy approach and nudge speeds, trail length and footprint lifetime. Its `_comment` field and **README.txt** explain the available settings and ranges. Keep the file valid JSON.

This version supports configuration presets rather than scripted or original DLL mods. Sound samples can be replaced in the application's **Assets/Sounds** folder; keep a backup before changing installed files.

### Older collections

**Your content → Import Legacy Content Folder** copies older pictures, text files or JSON note packs without overwriting current files.

Old `%LOCALAPPDATA%\DesktopGoose2\settings.json` preferences migrate once when Documents settings do not exist. Samples are seeded only when missing; removed samples are not recreated on every launch. Delivered windows are never saved.

## Troubleshooting and diagnostics

| Situation | What to try |
| --- | --- |
| You need the desktop back | Tap Escape for the cursor, or use **Ctrl+Alt+Q** to quit everything. |
| Toys or footprints are in the way | Use **Clear desktop objects** in the tray. |
| A picture doesn't appear | Check the supported format and limits above, then use **Your content → Reload content**. |
| A preset edit doesn't apply | Check that the JSON is valid. The last valid configuration remains active if an edit cannot be loaded. |
| You want to report a problem | Use **Your content → Copy diagnostics**, then attach the summary to a [bug report](https://github.com/MichaelYelavich/GooseOnTheLoose/issues/new/choose). Include the steps that caused it. |

Diagnostics contain application and performance information without custom content or paths. Error logs remain local and rotate across three files. Nothing is uploaded automatically. Review anything you choose to share.

**Current beta limits:** one selected display at a time; unsigned installer; further hardware and environment checks remain for mixed-DPI display changes, actual Documents redirection and startup after reboot. See [validation](../VALIDATION.md) for the measured checks and their limits, and the [roadmap](ROADMAP.md) for what comes next.

## Command-line reference

Pass a command to **GooseOnTheLoose.exe** to control the running instance. Launching another copy normally contacts the existing goose; `--quit` waits for cleanup. Old and renamed executable names share one instance after an upgrade.

| Commands | Purpose |
| --- | --- |
| `--come`, `--peas`, `--ball`, `--fetch`, `--note`, `--image`, `--honk` | Play and mischief |
| `--pause`, `--quiet`, `--menu`, `--quit` | Application controls |
| `--toolbox`, `--content-folder`, `--reload`, `--clear` | Tools and content |
| `--diagnostics` | Diagnostic summary; writes **diagnostics.txt** in the content folder when no game is running |
| `--seed-content` | Initialize missing content without starting gameplay |
| `--snapshot output.png` | Export goose artwork |

For development, `DESKTOPGOOSE2_DATA_DIR` overrides the content directory and gives the process a separate single-instance lock and command events. Normal launches use Documents. **runtime.json** is enabled only for tests or when `GOOSE_DIAGNOSTICS=1` is set.
