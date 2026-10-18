# Goose on the Loose — Beta Release 1

**0.4.0-beta.1**, the first public beta and the next iteration after Desktop Goose 2 0.3.1. Windows 11, .NET Framework 4.8. This is a prerelease.

## Improvements

- New name, with the same desktop-first geese, shared outfits, Toy Box, water, footprints and optional native content tools.
- Fix a crash when live content changes during an offscreen pickup. Deliveries capture their note and picture before leaving.
- Validate and prepare picture deliveries off the interface thread, with bounded caching and cancellation during cleanup.
- Save only edited notes, debounce saves, replace files atomically, and preserve external edits/deletions through conflict copies.
- Recover from unavailable content folders and watcher failures, retain valid settings/presets, and scan live content on a worker.
- Acquire single-instance ownership before initializing content. Secondary commands no longer reseed or migrate content.
- Correct legacy note-import totals and cancellation.
- Reuse native premultiplied rendering buffers and footprint drawing resources; hide the empty toy layer and skip unchanged paused frames.
- Add explicit diagnostics and bounded local logs, with no automatic uploads.
- Separate application, core, test and asset-generation projects; add pinned build dependencies, Windows CI, package checks, contribution docs and a roadmap.

## Install or upgrade

Download **GooseOnTheLoose-Beta1-Setup.exe**, or extract the entire **GooseOnTheLoose-Beta1-windows.zip** and run **GooseOnTheLoose.exe**. The source ZIP includes everything needed to build with the pinned SDK.

The installer is per-user and retains the previous installation folder on upgrade. Existing Documents/DesktopGoose2 content stays in place; fresh profiles use Documents/GooseOnTheLoose. Settings and content formats remain compatible. Upgrade-only compatibility executables keep old shortcuts working. Startup and desktop shortcuts remain optional and off by default. Uninstall retains custom content.

## Known limits

The installer is unsigned. The game currently uses one selected display. Hardware-specific mixed-DPI, display hot-plugging, actual redirected/network Documents, and reboot/sign-in startup execution remain unverified. Installer testing uses isolated paths and a separate product identity on the test host rather than a disposable VM. Native dialog tests are a local interactive release gate, separately reported from CI. Full evidence is in [VALIDATION.md](https://github.com/MichaelYelavich/GooseOnTheLoose/blob/main/VALIDATION.md).

Personality and play are the next feature priorities, followed by multi-monitor support. Report reproducible glitches through GitHub's bug form; the tray's Copy diagnostics command provides a summary without notes or pictures.
