# Desktop Goose 2 — 0.3.1

Released 18 October 2026. Install over 0.3.0; preferences, notes, custom pictures, and behavior presets are preserved.

- **Canada goose companion:** black head and neck, white cheeks, brown-grey body, dark beak and feet. Both geese now wear your selected scarf or sunhat, including during the reduced-motion dismissal.
- **Import crash fixed:** picture and legacy-folder dialogs no longer depend on an optional Toy Box that may have been closed. Errors show a recoverable explanation instead of shutting down the goose.
- **Safer picture imports:** PNG, JPEG, and all animated GIF frames are decoded before publication. Imports run in the background, can be cancelled, preserve duplicate filenames, and summarize skipped files. Existing 8 MB/8192-pixel limits remain; animations have a 500-frame/128-million-decoded-pixel limit.
- **Feet stabilized:** a speed change cannot alter the duration of an already active step. Water transitions rebuild the foot rig, and companion feet use its actual smaller scale.
- **Footprints stabilized:** landing position, angle, and size are retained. Cached, pixel-aligned stamps keep stationary prints steady while the overlay moves; changing goose size affects new prints only. Expiration, clearing, and quitting release cached images.
- **Text-only honk picture:** removed the decorative goose to avoid confusion with a second desktop goose. Upgrade migration replaces only exact original stock images, including renamed copies. Edited and unrelated pictures are preserved.
- **Small fixes:** water editing handles and the Escape countdown remain within the selected display; modal content tools release the cursor and restore the previous pause state; import cancellation handles repeated close notifications safely.

The goose remains a desktop toy with optional native tools. No settings format change or new setup screen is required. See VALIDATION.md for regression results and hardware/clean-VM verification limits.
