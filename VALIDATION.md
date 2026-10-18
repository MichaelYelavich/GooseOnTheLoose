# Desktop Goose 2 0.3.1 — verification

Test host: Windows build 26300, .NET Framework 4.8. The patch retains the desktop-first runtime and existing settings format. Research references remain in RESEARCH.md; user-facing changes are in RELEASE_NOTES.md.

## Confirmed causes and fixes

The reported import crash is recorded in the existing application log as an ObjectDisposedException from CommonDialog.ShowDialog: a closed Toy Box was passed as owner. Picture and legacy-folder dialogs now use a separate hidden owner lasting until application shutdown. Dialog-opening failures show a recoverable message. Gameplay and cursor stealing are suspended during modal tools, with the previous pause state restored afterward.

Foot-step duration previously changed between 0.1 and 0.2 seconds during an active stride. Crossing the speed threshold could move the interpolation backwards. Duration is now fixed at step start. Companion feet use the companion's effective scale; water transitions and paused size changes rebuild the rig. Deposited footprints snapshot position, angle, coating and scale and are rendered through bounded, pixel-aligned cached stamps.

The companion uses Canada goose colors and shares the appearance preference. The honk picture is centered text only. Existing stock art is recognized by its original SHA-256; atomic migration preserves edited/unrelated files and is safe to repeat.

## Application regression suite

**110 checks pass** in artifacts/desktop-self-test.txt. These include the original 71 checks for movement, toys, water, both geese's coatings, footprint caps/fading, cursor release, Escape timing, shutdown, content reload, settings recovery, legacy import and direct drag-controller behavior.

Patch checks cover braking and acceleration across stride thresholds, sharp turns, companion foot spacing, paused size changes, stationary footprint pixel equality across moving/resized overlay crops, fading and cache disposal, water transitions, every appearance/size combination, countdown/surface handles at screen edges, PNG/JPEG/animated-GIF imports, corrupt/locked files, duplicate names, cancellation/temp cleanup, stock-art migration and preservation, and dialog-owner survival across repeated Toy Box disposal.

A visual appearance sheet is saved as artifacts/companion-appearance-0.3.1.png. Native visual inspection confirmed the Canada goose palette and matching hats/scarves. Inspection also caught clipped text in the old honk asset; the replacement uses a centered pixel-sized font and fits the image.

## Native dialogs, rendering and resource checks

**14 native checks pass** in artifacts/desktop-native-smoke.txt:

- Existing overlay styles, native alpha presentation, offscreen return, separate toy interaction layer, native Toy Box/note controls and Windows audio backend.
- Real Windows file pickers open and cancel with no Toy Box, after closing it, and after repeated disposal. The real legacy folder picker also cancels safely.
- A simulated dialog-opening failure shows one recoverable message and restores gameplay. Immediate quit during a real file picker closes the picker, its owner and every overlay. Quit during import progress cancels its worker.
- Maximum 256 footprints, 16 objects and both geese render for 180 dual-layer frames with GDI handles **110 → 110**. The standard 300-frame assortment remains **32 → 32**.
- Twenty-four validated PNG/animated-GIF gift windows open/render/close with GDI handles **98 → 98**. PictureBox controls stop animation before their images and streams are disposed.

The native shutdown test exposed a repeated-close cancellation callback after disposal; that callback is guarded and the regression now passes. Native tests fail with a saved error report instead of waiting indefinitely at a framework exception dialog.

## Installer and archive checks

**17 installer checks pass** in artifacts/installer-verification.txt. The test rebuilds the actual 0.3.0 source archive and its installer, installs it under a separate test identity, starts the old application, and upgrades to 0.3.1. Checks confirm graceful shutdown, the new executable version, preserved settings/custom notes, stock-art migration, preservation of a customized honk.png, shortcut options, uninstall cleanup, and retained content. A separate fresh 0.3.1 installation seeds text-only art with desktop/startup options off, then uninstalls cleanly.

Tests use isolated application/data directories containing spaces and a separate installer AppId on the current Windows host. Test shortcuts and installer registration are removed by the uninstaller. Source packages include research metadata and the embedded stock-art test fixture, while excluding compiler installations, research checkouts and test user data.

artifacts/package-verification.txt records extraction of the 0.3.1 portable ZIP into a path with spaces, executable version/hash verification, both test suites running from that extracted copy, source ZIP extraction/rebuild, and archive-content checks.

## Remaining limits

A disposable Windows VM/Windows Sandbox is unavailable on this host. Installer tests isolate paths and product identity on the current OS; actual Documents known-folder redirection, network-backed Documents, reboot/sign-in startup execution, Windows shutdown during a held native window drag, mixed-DPI multi-monitor hardware and display hot-plugging remain unverified.

Drag-controller tests exercise the production code directly; end-to-end physical dragging of every item across the live desktop is not claimed. Escape timing uses the production controller rather than injecting Escape into the computer-use tool. The animation changes and raster invariants are covered by tests, but every intermittent compositor or driver-specific graphical issue cannot be ruled out. Audible quality has not been independently judged.
