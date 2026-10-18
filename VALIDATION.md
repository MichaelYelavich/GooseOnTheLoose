# Beta Release 1 — validation

Release: Goose on the Loose 0.4.0-beta.1. Test host: Windows 11 build 26300, x64, .NET Framework 4.8. Developer SDK: 10.0.402; Inno Setup: 6.7.3. The application and test runner report the real Windows version through their manifest.

## Regressions and native checks

139 application checks pass, including all 110 pre-beta checks. New coverage includes delivery after live lists disappear, profile precedence, atomic note saving, external edits and deletion, locked notes, unchanged-editor close, watcher filtering and unavailable/recovered folders, legacy note totals/cancellation, background picture preparation and cache limits, rotating logs, diagnostics privacy, and secondary-instance startup/quit behavior.

The delivery regression was run before the fix and reproduced a DivideByZeroException. Deliveries now snapshot their payload at task start rather than indexing live lists after leaving the desktop.

14 native Windows checks pass. They cover click-through/no-activate styles, offscreen return, interactive toy controls, full-assortment rendering, PNG/GIF windows and disposal, real file/folder dialog cancellation after repeated Toy Box cycles, recoverable dialog errors, quit during a picker, and cancellation during import progress.

The appearance sheet and native Toy Box rendering were visually inspected. Existing tests cover every shared appearance/size, Canada-goose proportions, swimming transitions, stationary footprint pixels across crop changes, exact five-second Escape timing/cancellation, simultaneous bursts, reduced motion, paused cleanup, and immediate quit. These controller/native checks do not claim physical mouse/keyboard testing of every route or driver-specific compositor behavior.

## Performance

The same benchmark harness was compiled against the original 0.3.1 binary and the beta. Each scenario uses 600 dual-layer frames at 1280×752, with warmup and the same seeded simulation. Full load has two geese, 16 objects, 256 replenished footprints, and editing handles. Measurements are local offscreen workloads, not universal hardware guarantees.

| Scenario | 0.3.1 p95 frame | Beta p95 frame | 0.3.1 CPU total | Beta CPU total |
| --- | ---: | ---: | ---: | ---: |
| One goose | 2.409 ms | 0.413 ms | 859 ms | 125 ms |
| Two geese | 3.217 ms | 0.721 ms | 1,281 ms | 234 ms |
| Full load | 35.667 ms | 21.680 ms | 16,406 ms | 8,359 ms |

Full-load GDI/USER counts remain unchanged across each run. Gen-0 collections fall from six to two in the full-load comparison. Private memory grows from 38 to 44 MB for the baseline and 37 to 43 MB for the beta during this short run. Native buffers retain two extra GDI objects while active rather than recreating them every frame.

The real 30-minute paced maximum-load soak is a release gate. Its final measured outcome will be recorded here before publication.

## Installer and packages

18 installer checks pass against a rebuilt actual 0.3.1 baseline, using the same test AppId as the previous installer. They cover previous-directory reuse, running-app shutdown, updated legacy compatibility binary, renamed/optional shortcuts, preserved custom pictures/notes/settings, legacy/new single-instance behavior, fresh install, uninstall, and retained content. Test paths contain spaces and a simulated redirected Documents destination. The test removes its own shortcuts and installer registration.

Extracted portable/source checks and GitHub-hosted Windows validation are required before publication; final results are recorded in the release commit.

## Remaining verification limits

A disposable Windows VM/Sandbox is unavailable. Tests use isolated paths and a separate product identity on this host. Actual Windows known-folder redirection, network-backed Documents, reboot/sign-in startup execution, Windows shutdown during a held native window drag, ARM64, mixed-DPI multi-monitor hardware, and display hot-plugging remain unverified. The current application uses one selected display. Native modal tests are an explicit local gate, not silently treated as passing in noninteractive CI.

The installer is unsigned. No automatic telemetry is sent. Test logs, fixtures generated during runs, profile paths, and local user content are excluded from release packages and Git. Historical 0.3.1 evidence is preserved separately in docs.
