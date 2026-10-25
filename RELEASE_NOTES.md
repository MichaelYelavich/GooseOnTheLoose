# Goose on the Loose — Beta Release 2

**0.5.0-beta.1**, the second public beta and the next iteration after Desktop Goose 2 0.3.1. Windows 11, .NET Framework 4.8. This is a prerelease.

## What's new

- **Two geese.** Invite an optional Canada goose companion from Options. Both geese wander, honk, fetch, deliver and play, and either one can start a game. Two is the whole flock: no third goose will ever join.
- **Shared scenes.** The pair play together: passing a ball back and forth, taking turns fetching a stick, bracing together on a tug rope, sharing a pond, resting nearby or getting into coordinated trouble. Either goose can invite; the other may accept, decline or get distracted.
- **An invisible bond.** Geese that spend time playing together grow closer, and that closeness carries over between launches. Warmer geese seek each other out more and are likelier to trade notes back and forth.
- **Wardrobe.** Dress both geese from the tray's Wardrobe window, with live previews of each: sunhat or beanie, scarf or bow tie, round glasses, in any combination. Outfits stay linked by default so one change dresses both; unlink a slot to dress one goose on its own.
- **Three modes.** Passive keeps peaceful play, swimming and shared scenes while stopping pranks, unsolicited deliveries and window or cursor interference. Normal is the everyday mischief: deliveries, honks, following or poking at the cursor without capturing it, plus small nudges to the window you are using. Chaos turns everything up, with more frequent pranks, brief cursor captures and drags of windows you are not using.
- **Window manners.** In Normal the geese may give the active window a small nudge while you work. In Chaos they may also drag an inactive window to a new spot on the same display. They never resize windows, change focus or reorder windows, and windows stay where the geese leave them. Grabbing, moving or resizing a window yourself makes the goose let go of it at once. Passive stops all window interference.
- **Cursor grabs (Chaos only).** A provoked goose — random attacks start disabled, and stay off unless you turn them on under Options — may briefly hold the cursor, never longer than 1.4 seconds. Click the primary mouse button to take it back at once, or tap Escape. Holding Escape for five seconds still dismisses the geese entirely.

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

Download **GooseOnTheLoose-Beta2-Setup.exe**, or extract the entire **GooseOnTheLoose-Beta2-windows.zip** and run **GooseOnTheLoose.exe**. The source ZIP includes everything needed to build with the pinned SDK.

The installer is per-user and retains the previous installation folder on upgrade. Existing Documents/DesktopGoose2 content stays in place; fresh profiles use Documents/GooseOnTheLoose. Settings and content formats remain compatible. Upgrade-only compatibility executables keep old shortcuts working. Startup and desktop shortcuts remain optional and off by default. Uninstall retains custom content.

## Known limits

The installer is unsigned. The game currently uses one selected display. Hardware-specific mixed-DPI, display hot-plugging, actual redirected/network Documents, and reboot/sign-in startup execution remain unverified. Installer testing uses isolated paths and a separate product identity on the test host rather than a disposable VM. Native dialog tests are a local interactive release gate, separately reported from CI. Full evidence is in [VALIDATION.md](https://github.com/MichaelYelavich/GooseOnTheLoose/blob/main/VALIDATION.md).

Personality and play are the next feature priorities, followed by multi-monitor support. Report reproducible glitches through GitHub's bug form; the tray's Copy diagnostics command provides a summary without notes or pictures.
