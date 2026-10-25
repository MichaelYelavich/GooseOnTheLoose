# Beta Release 2 — validation

Release: Goose on the Loose 0.5.0-beta.1. Test host: Windows 11 build 26300, x64, .NET Framework 4.8. Developer SDK: 10.0.402; Inno Setup: 6.7.3. The application and test runner report the real Windows version through their manifest.

Beta 1 evidence below (142 application checks, 17 native checks, installer and package checks against 0.3.1) is retained as the baseline: the Beta 2 change set carries Cards 1–4 behavior work on the same build and packaging pipeline. Beta 2 packaging verification follows in its own section at the end.

## Regressions and native checks

142 application checks pass, including all 110 pre-beta checks. New coverage includes delivery after live lists disappear, profile precedence, atomic note saving, external edits and deletion, locked notes, unchanged-editor close, watcher filtering and unavailable/recovered folders, legacy note totals/cancellation, background picture preparation and cache limits, rotating logs, diagnostics privacy, and secondary-instance startup/quit behavior.

The delivery regression was run before the fix and reproduced a DivideByZeroException. Deliveries now snapshot their payload at task start rather than indexing live lists after leaving the desktop.

17 native Windows checks pass. They cover click-through/no-activate styles, offscreen return, interactive toy controls, full-assortment rendering, PNG/GIF windows and disposal, real file/folder dialog cancellation after repeated Toy Box cycles, recoverable dialog errors, quit during a picker, and cancellation during import progress.

The appearance sheet and native Toy Box rendering were visually inspected. Existing tests cover every shared appearance/size, Canada-goose proportions, swimming transitions, stationary footprint pixels across crop changes, exact five-second Escape timing/cancellation, simultaneous bursts, reduced motion, paused cleanup, and immediate quit. Native tests also exercise 240 buffer reallocations, the hidden empty toy layer, background image delivery, and cancellation without stale windows. These controller/native checks do not claim physical mouse/keyboard testing of every route or driver-specific compositor behavior.

## Performance

The same benchmark harness was compiled against the original 0.3.1 binary and the beta. Each scenario uses 600 dual-layer frames at 1280×752, with warmup and the same seeded simulation. Full load has two geese, 16 objects, 256 replenished footprints, and editing handles. Measurements are local offscreen workloads, not universal hardware guarantees.

| Scenario | 0.3.1 p95 frame | Beta p95 frame | 0.3.1 CPU total | Beta CPU total |
| --- | ---: | ---: | ---: | ---: |
| One goose | 2.409 ms | 0.413 ms | 859 ms | 125 ms |
| Two geese | 3.217 ms | 0.721 ms | 1,281 ms | 234 ms |
| Full load | 35.667 ms | 21.680 ms | 16,406 ms | 8,359 ms |

Full-load GDI/USER counts remain unchanged across each run. Gen-0 collections fall from six to two in the full-load comparison. Private memory grows from 38 to 44 MB for the baseline and 37 to 43 MB for the beta during this short run. Native buffers retain two extra GDI objects while active rather than recreating them every frame.

The real 30-minute paced maximum-load soak passes: **43,948 frames over 1,800.0 seconds**, p50 **10.865 ms**, p95 **17.806 ms**, CPU total **527,906 ms**, GDI **34 → 34**, USER **25 → 24**, and private memory **37 → 44 MB** after a final collection. Periodic samples plateau around 43–46 MB after warmup. The run maintains two geese, 16 objects and 256 replenished footprints, and repeatedly opens/renders/closes picture windows. Gen-0 collections: 84. The final rendering code is unchanged from the soaked build; subsequent changes add tests, formatting and deterministic-build settings.

## Installer and packages

18 installer checks pass against a rebuilt actual 0.3.1 baseline, using the same test AppId as the previous installer. They cover previous-directory reuse, running-app shutdown, updated legacy compatibility binary, renamed/optional shortcuts, preserved custom pictures/notes/settings, legacy/new single-instance behavior, fresh install, uninstall, and retained content. Test paths contain spaces and a simulated redirected Documents destination. The test removes its own shortcuts and installer registration.

Six extracted-package checks pass: portable hash/version, seed/start/quit, byte-identical application/core rebuilds from archived source, all application regressions from the extracted source, and generated/private-file exclusions for both ZIPs. [GitHub-hosted Windows validation passed](https://github.com/MichaelYelavich/GooseOnTheLoose/actions/runs/39181985106) on a fresh Windows Server 2025 runner (build 26100): 142 application checks, 18 installer checks, and six package checks. Reports are retained as CI artifacts. Interactive native-dialog checks are explicitly marked skipped in hosted CI and are covered by the separate Windows 11 local run. The release workflow repeats these hosted gates for the version tag before publishing.

## Remaining verification limits

A disposable Windows 11 VM/Sandbox is unavailable locally. Local tests use isolated paths and a separate product identity; hosted checks run in a separate Windows Server 2025 runner. Actual Windows known-folder redirection, network-backed Documents, reboot/sign-in startup execution, Windows shutdown during a held native window drag, ARM64, mixed-DPI multi-monitor hardware, and display hot-plugging remain unverified. The current application uses one selected display. Native modal tests are an explicit local gate, not silently treated as passing in noninteractive CI.

The installer is unsigned. No automatic telemetry is sent. Test logs, fixtures generated during runs, profile paths, and local user content are excluded from release packages and Git. Historical 0.3.1 evidence is preserved separately in docs.

## Beta 2 packaging verification (Card 6, local, 2026-10-25)

Version bump only (0.4.0-beta.1 → 0.5.0-beta.1, `Version.props` plus player-facing strings; installer AppId, IPC identity and data-directory behavior unchanged). Build: locked restore, Release build, asset generation, format check clean, **417 application checks and 17 native checks pass** on this host. Packages rebuilt with Inno Setup 6.7.3 (hash- and signature-verified per `tools/install-inno.ps1`) into `.work/release/`:

- `GooseOnTheLoose-Beta2-Setup.exe` — 2,248,952 bytes, SHA-256 `9B2E3D1C7EB22758865FE74BCC0A291719381EB9E6C0C63CEAA74E1F9A9A`
- `GooseOnTheLoose-Beta2-windows.zip` — 263,606 bytes, SHA-256 `31BB05137D15CE285B77FCDB4BDB4B1FF6A0B6C230A7881EE5F01CA17E1C99`
- `GooseOnTheLoose-Beta2-source.zip` — 329,544 bytes, SHA-256 `E237D1D9653D4A36AC316B2DB4CEC39743E3595A42794ED5BC29FFBA`

Both ZIPs contain no `private/`, `.work/`, `artifacts/`, `bin/`, `obj/` or `.git/` entries and no test runtime in the portable game; the source archive holds exactly the 101 tracked files at the release commit. The portable executable is byte-identical to `dist/GooseOnTheLoose.exe` (MD5 `9e73f012898b0989a40324768162ca35`).

Upgrade: the Beta 2 installer installed in place over a genuine Beta 1 install (same per-user directory, same uninstall registration, DisplayVersion updated to 0.5.0-beta.1). Seeded settings, a sentinel custom note and a custom picture hash were unchanged by the upgrade, and `--seed-content` rewrote nothing existing. A second Beta 2 upgrade over a live running instance shut the instance down via the IPC `--quit` handshake (no surviving process, files replaced cleanly, `--seed-content` exit 0); holding the binary open makes setup abort before any replacement with "Please quit Goose on the Loose from its tray menu, then try again" (exit 7), leaving the install untouched.

Not re-run in this card: hosted CI gates, the 30-minute soak, and the full test-identity installer suite (`tools/verify-installer.ps1`) — unchanged installer script, re-verified at release time per the checklist.
