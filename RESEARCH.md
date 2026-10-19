# Desktop Goose research — Goose on the Loose

Goose on the Loose is an independent spiritual successor to Samperson's Desktop Goose. This research records the original game, recovered source and mod ecosystem that informed the project. Historical comparisons below use the earlier working name **Desktop Goose 2**; current player and build documentation uses **Goose on the Loose**.

Research date: **13 October 2026**. This investigation combined the creator's page, GitHub repository discovery, inspection of eight checkouts, and GitHub comparisons of the four forks of the public decompilation. Repository SHAs and commit dates are recorded in `research/sources.json`; the discovery response is in `research/github-search.json`. Downloaded reference checkouts remain under `research/`, excluded from this project's distribution.

## What the original actually is

Samperson's Desktop Goose is a desktop toy built around an autonomous, deliberately inconvenient animal. Its official page lists mouse theft, mud, messages and memes. It lists Windows v0.31 and macOS v0.22; v0.3 introduced the mod API and custom note text, and v0.31 added the Autumn mod. Those are distinct versions, and the Windows mod system should not be assumed to work on macOS. [Creator's page](https://samperson.itch.io/desktop-goose)

The useful original-code reference is **elliotgsy/DesktopGooseDecompiled**. Its source headers identify a JetBrains decompilation of the Windows **v0.21** executable. This is recovered source, rather than the creator's original annotated development tree. It exposes real implementation details, but does not establish every internal detail of v0.31. The later API source shipped in TheOrlando's mod repositories provides a separate view of the v0.3 mod architecture. [Decompilation repository](https://github.com/elliotgsy/DesktopGooseDecompiled), [API source](https://github.com/TheOrlando/DesktopGooseMod_Ball/blob/3f66dc8bc7f3eaf0e9ce8600e1dd4339b1c5ee47/GooseModdingAPI/Exports.cs)

## Original code: the important mechanisms

### Window and loop

`Program.Main` creates a borderless topmost WinForms window sized to the primary working area. A coral transparency key removes the background. Extended layered and transparent styles make the window pass through mouse input. `Application.Idle` drives repeated invalidation with an 8 ms sleep. Rendering also advances the simulation, so update and presentation are coupled. [Program.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/GooseDesktop/Program.cs)

`SamEngine.Time` measures elapsed time with a stopwatch while exposing a constant 1/120-second simulation delta. That combination can make motion depend on how often rendering actually occurs. Desktop Goose 2 uses a timer and measured deltas, with a bounded simulation step and a separate correction for delayed focus ticks. [Time.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/SamEngine/Time.cs)

### Behavior, movement and rig

`TheGoose` contains a static state machine. Tasks cover wandering, mouse chasing, collecting windows and tracking mud. Its weighted eight-entry deck includes two mud entries, two meme entries, one note entry and three mouse entries; settings filter random attacks. Walk, run and charge speeds are 80, 200 and 400. Mouse chasing progresses through seeking, dragging and deceleration; dragging uses a small `Cursor.Clip` rectangle near the beak. Window collection walks to an edge, waits, then positions a form relative to the head while returning.

The goose is procedural geometry rather than an animated sprite. Body, neck and head points depend on direction and neck extension. The feet alternate stepping after exceeding their home-distance threshold; cubic interpolation and slight overshoot make steps feel planted. Rendering uses rounded strokes and ellipses, layered with shadow, feet, body, neck, head and eyes. These details explain why the original has more personality than a sprite merely sliding across the screen. [TheGoose.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/GooseDesktop/TheGoose.cs)

`Deck` reshuffles indices and draws without replacement. This gives variety while preserving repeated weighted entries. Desktop Goose 2 retains a small shuffled idle-activity deck; deliberate player commands take precedence. [Deck.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/SamEngine/Deck.cs)

Mud uses a fixed footprint ring, with an 8.5-second lifetime and one-second shrink interval. Desktop Goose 2 instead bounds its planted-footprint list and expires footprints after 10 seconds. Both approaches avoid accumulating permanent desktop marks. [FootMark.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/GooseDesktop/FootMark.cs)

### Configuration and exit

The v0.21 `GooseConfig` parses `key=value` lines and assigns reflected fields. Its defaults include 20–40-second wandering and disabled random attacks. Bad or mismatched configurations trigger replacement and dialogs. Desktop Goose 2 exposes settings in the tray menu and keeps a previous-save backup. [GooseConfig.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/GooseDesktop/GooseConfig.cs)

`MainGame.Update` polls Escape, accumulates a quit indicator and eventually exits. Desktop Goose 2 keeps a global hold-to-quit gesture, adds a tray exit and Ctrl+Alt+Q, and immediately stops its optional cursor tug on Escape. [MainGame.cs](https://github.com/elliotgsy/DesktopGooseDecompiled/blob/0b01b89c5fdecce07f9156398861564d7500cc22/GooseDesktop/MainGame.cs)

## Mods, forks and newer implementations

| Project | Evidence inspected | Finding and effect on Desktop Goose 2 |
| --- | --- | --- |
| [TheOrlando / Ball](https://github.com/TheOrlando/DesktopGooseMod_Ball) | `DefaultMod/ModMain.cs`, `ChargeTenSecondsTask.cs`, `GooseModdingAPI/Exports.cs` | A moving ball responds to the cursor and goose, reflects at primary-screen edges and triggers a chase task. Its integer per-tick movement and fixed decrement suggested using elapsed-time movement and exponential drag in the sequel. |
| [TheOrlando / Toys](https://github.com/TheOrlando/DesktopGooseMod_Toys) | `PetGoose/Main.cs`, `Ball.cs`, `Stick.cs`, `Bed.cs`, `Laser.cs`, chase/return/sleep tasks | Registers four props through pre/post update and render hooks. Stick retrieval attaches the prop to the head and returns toward the mouse; bed interaction changes the goose into a sleeping task. Desktop Goose 2 gives fetch an explicit pickup/return/completion cycle, desktop rest, sleep and chase. |
| [JonDoge42 / DesktopGooseModsSource](https://github.com/JonDoge42/DesktopGooseModsSource) | Nametag `ModMain.cs`; menu `ModMain.cs` and form source | Name configuration and render hooks demonstrate how little changes can improve attachment. The menu entry point calls `Application.Run` during mod initialization, making UI ownership worth treating carefully. Desktop Goose 2 has one application loop, native tray controls. |
| [DesktopGooseUnofficial / launcher](https://github.com/DesktopGooseUnofficial/launcher) | README and repository layout | An archived mod installer; its README documents folder placement, enabling mods and restarting. It is ecosystem infrastructure, not original game source. Desktop Goose 2 bundles its mechanics and loads simple local content packs without a separate launcher. |
| [claudepc42 / PyGoose](https://github.com/claudepc42/DesktopGoose-PyGoose) | `goose.py`, `overlay.py`, `props/physics.py` | A Python/PyQt6 continuation with separate behavior modules, props and Windows overlay code. Its stateful prop physics confirms the value of treating objects as simulation entities. Desktop Goose 2 keeps its simulation independent of WinForms rendering, while requiring no Python installation. |
| [RealEmmettS / goose](https://github.com/RealEmmettS/goose) | `src/runtime/windows.rs`, `crates/honk-platform-windows/src/lib.rs` | A Rust remake with a native layered-window backend, per-monitor windows, DPI handling and separated platform capabilities. This provided a useful comparison for alpha compositing and resource ownership. Desktop Goose 2 implements its own smaller C# backend and uses one selected monitor. |
| [zhanghanboke / DesktopGoose-Plus](https://github.com/zhanghanboke/DesktopGoose-Plus) | README and package layout | Its documented features include companions, skins, hotkeys, tray controls, a focus timer and delivery cooldowns. The inspected checkout packages binaries rather than readable mod implementation source, so these are documentation-level findings. Companionship and quiet time are worthwhile directions for a sequel. |
| [NguyAnhQuan / DesktopGoose](https://github.com/NguyAnhQuan/DesktopGoose), [TogoFire / DesktopGoose](https://github.com/TogoFire/DesktopGoose) | GitHub file listings and README | Distribution mirrors/bundles containing executables and assets. A repository called DesktopGoose is not automatically a source release or a development fork. They were not used as implementation dependencies. |

The v0.3 API exposes `IMod.Init`, `GooseTaskInfo`, per-task data, injection events around update/render/rig calculation and delegates for selecting tasks, setting speed and playing a honk. That explains how the Toys and Ball mods extend the original without replacing its renderer. Desktop Goose 2 uses a fresh simulation API internally; it does **not** claim DLL compatibility with those mods. [Exports.cs](https://github.com/TheOrlando/DesktopGooseMod_Toys/blob/ccf0335d7e8c19abeb6833507a51c22a8d57c8bb/GooseModdingAPI/Exports.cs)

### Actual forks of the decompilation

GitHub listed four forks: `Shl26th/DesktopGooseDecompiled`, `laipuran/DesktopGooseDecompiled`, `Niborj/DesktopGooseDecompiled` and `F0x45N/DesktopGooseDecompiled`. Each default branch compared **identically** to the upstream default branch: zero commits ahead and zero behind. They add no distinct implementation to study in that branch comparison. This finding is limited to those default branches, not a claim about every branch ever created. Metadata is saved in `research/decompiled-forks.json`; comparison evidence is in `research/fork-comparison.json`. [Upstream forks](https://github.com/elliotgsy/DesktopGooseDecompiled/forks)

## The desktop sequel that was built

**Desktop Goose 2** launches directly onto the Windows desktop. It has no main game window. A transparent, click-through, non-activating overlay draws the goose over the user's existing applications. Controls live in a native system-tray menu; note and image windows exist only as things the goose brings onto the desktop.

The rejected pond prototype was removed from the active source and distribution. Its dashboard, progression, quests, currencies, unlock screens, custom buttons, speech panels, and main Form are absent from this build. An `ApplicationContext` owns the overlay and tray. A message-only native window receives shortcuts and does not display an interface.

The procedural goose follows the original body/neck/head construction and proportions. Movement turns its body and head; feet alternate planted steps with cubic interpolation and forward placement. Mud marks come from actual foot landings. Original Desktop Goose honks, bite, mud and footstep samples restore its sound. The source of each audio set is recorded in `Assets/Sounds/PROVENANCE.md`.

The original's core behaviors remain central: autonomous wandering, honking, reacting to being poked, chasing and taking the cursor, tracking mud, and fetching text/image windows from beyond a screen edge. During a delivery, the new native window is attached to the beak by its edge and moved as the goose walks back. Closing a goose-created window can provoke a chase. Cursor theft releases within 1.4 seconds, and immediately on Escape, feeding, pause, quiet mode or disabling cursor stealing.

The expansions happen directly on the desktop: drop peas to distract the goose, throw a ball, play stick fetch, invite a second goose, choose an optional scarf or hat, and use quiet mode. These are available immediately. A native menu supplies pause, cleanup, sound and mischief options, display selection and quit. Text packs support JSON and plain text files; image gifts support PNG/JPEG/GIF.

## Scope and practical limits

The shipped target is Windows 11 with .NET Framework 4.8 or later. It is a portable application with no external package dependency. It roams one selected display's working area, including displays with negative desktop coordinates. Seamless travel between monitors and mixed-DPI/hot-plug configurations remain outside this build's verified scope.

The original v0.21 decompilation was inspected directly; it is not represented as complete v0.31 development source. Downloaded reference executables and mods were not run. The new runtime includes original audio samples, but does not require the original executable or DLL mods. Verification of the simulation, native overlay, live delivered windows and shortcuts is recorded in `VALIDATION.md`.

The platform choices were checked against Microsoft's documentation for [layered windows and mouse pass-through](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features), [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow), and [Framework versions included with Windows](https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies).

## October 18 expansion: 0.3.0

The desktop-first runtime now has an optional native Toy Box, sixteen independently identified objects, wet/muddy foot coatings per goose, and a separate interactive toy layer. Ponds are desktop surfaces, rather than a game screen. Five seconds of Escape triggers a simultaneous cartoon burst; all shutdown routes use the same cleanup. Custom pictures, plain-text notes, documented behavior presets and preferences live in the Windows Documents known folder, with debounced reload and previous-valid configuration recovery. The distribution now includes a per-user Inno Setup installer as well as portable/source ZIPs. See README.md and VALIDATION.md for the current behavior and exact verification limits; the source investigation above remains the original October 13 evidence.

## Stability patch: 0.3.1

The companion now uses Canada goose colors and shares the selected appearance. Investigation of the reported import crash found a disposed Toy Box being passed as the file dialog owner; content tools now have an independent owner. Step duration is fixed at stride start, deposited footprints retain their landing geometry and cached pixels, and stock honk art no longer contains a goose illustration. Migration identifies the old bundled picture by SHA-256 and preserves custom art. Current regression evidence is in VALIDATION.md.
