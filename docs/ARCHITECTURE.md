# Architecture

The process owns an ApplicationContext, not a main game window. The tray, hidden command window, optional tools, delivery windows, and two native layered overlays are owned by DesktopSession.

## Projects and ownership

- **Goose.Core:** vectors, simulation, settings/content models, behavior presets, profile selection, atomic note saves, and dismissal timing. World changes occur on the UI thread with a bounded simulation step.
- **GooseOnTheLoose:** Windows input, tray/commands, native rendering, dialogs, content monitoring, image preparation, audio, and cleanup.
- **Goose.Tests:** existing regressions, beta regressions, native dialog/render tests, and a performance/soak harness. No tests are embedded in the game.
- **Goose.AssetGenerator:** generates the icon and text-only sample picture during builds. Its generated files are not source dependencies.

DesktopOverlay owns a reusable top-down premultiplied DIB and memory DC. The background is click-through; the separate toy layer receives input only where it has nontransparent pixels. Water receives input only through editing handles. Footprint stamps snapshot landing geometry, remain independent of overlay crops, and expire with the model.

## Background work

FileSystemWatcher events only queue a reload. Debounced scans run on a worker; a completed snapshot is applied on the UI thread. Explicit reload and initialization also have a synchronous path. Settings and behavior validation retain the last valid values. Delivery text/path are captured when a task begins, so replacing live lists cannot invalidate an in-flight pickup.

Picture preparation validates and decodes off the UI thread. A serialized worker cache holds at most four validated byte arrays and 16 MB. Each prepared image/stream is transferred to one window and disposed after its PictureBox. Request revisions and cancellation prevent stale windows appearing after cleanup. Import workers use temporary files and publish with a rename.

The note editor tracks dirty text, debounces saves, and atomically replaces unchanged originals. If the original differs or disappears, it saves a conflict copy. This is best-effort conflict detection, not a cross-application transactional file lock.

## Compatibility and shutdown

Documents selection prefers an initialized GooseOnTheLoose profile, otherwise an existing DesktopGoose2 collection, otherwise a fresh GooseOnTheLoose folder. DESKTOPGOOSE2_DATA_DIR remains the isolated-test override. Saved JSON stays at Version 2.

Installer AppId and default command/mutex prefix retain DesktopGoose2 for upgrades. Renamed and legacy executable names share that identity. Only upgraded installations receive the compatibility executable.

All quit routes end at ExitThreadCore. Five-second Escape adds an effect before cleanup; immediate quit skips it. Shutdown cancels background work, closes owned dialogs, suppresses retaliation, disposes windows/render resources, saves preferences, and removes the tray icon.

Diagnostics are local and explicit. Logs rotate across error.log and two backups; copied summaries exclude paths/content/log text. runtime.json is written atomically only for isolated tests or when GOOSE_DIAGNOSTICS=1.
