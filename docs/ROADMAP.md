# Where the geese go next

Goose on the Loose is an independent spiritual successor to Desktop Goose. The aim is a more playful, expressive desktop companion that stays easy to live with.

[← Back to the README](../README.md#roadmap) · [Report a bug or suggest a feature](https://github.com/MichaelYelavich/GooseOnTheLoose/issues/new/choose)

These are priorities, not a dated release schedule. Beta feedback will shape the order within each stage. Planned items below are not claims about features already available.

## Here now · Beta Release 1

Two desktop geese, shared outfits, seven kinds of toys and water, wet and muddy footprints, picture and note delivery, native tray tools and editable content. The game works on one selected display at a time.

The current foundation includes reproducible builds, automated Windows checks, bounded image preparation, rotating local logs and opt-in diagnostics. See [release notes](../RELEASE_NOTES.md) and [validation](../VALIDATION.md) for the shipped changes and evidence.

## Next · Personality and play

Make time with the existing geese more interesting before expanding the Toy Box.

- **Individual habits:** more distinct reactions and moments of curiosity, with clearer interactions between the white goose and Canada goose.
- **Better play:** richer fetch, ball, duck and water reactions using the toys already present.
- **Readable movement:** more expressive procedural animation, smoother transitions and thoughtful reduced-motion behavior.
- **A dependable companion:** prioritize reproducible crashes and graphical issues; measure rendering and resource use locally before accepting performance changes.

These changes should keep the geese directly on the desktop, with optional controls in the tray and Toy Box.

## Then · Multi-monitor support

Move beyond the current selected-display model once movement and placement work across real hardware setups.

- Travel between displays and sensible placement of toys, water and delivered windows.
- Mixed-DPI scaling and displays positioned to the left of or above the main screen.
- Monitor disconnects, reconnects, resolution changes, sleep and resume.
- A dedicated hardware test matrix covering those transitions, including overlay bounds and desktop click-through behavior.

This work needs native Windows testing across more than one display configuration. It should preserve existing content and preferences.

## Exploring · Content and customization

Possible later improvements include named picture and note collections, easier organization and more documented behavior presets. These ideas need feedback and design work before becoming release commitments.

Scripted mods and automatic updates are not current beta features. A required dashboard is not part of the desktop companion's direction.

## Help shape it

For bugs, include the steps to reproduce, Windows version, display scaling and whether one or both geese were active. The tray's **Your content → Copy diagnostics** command can help; it never uploads anything automatically.

For feature ideas, describe the moment of play you want to create or the friction you want to remove. [Open an issue](https://github.com/MichaelYelavich/GooseOnTheLoose/issues/new/choose), or see [CONTRIBUTING.md](../CONTRIBUTING.md) if you want to work on the source.
