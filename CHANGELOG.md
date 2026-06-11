# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.3] - 2026-06-11

### Added

- Per-action `volume` field on audio actions (0–1 multiplier on top of the bus volume),
  with a Volume row in the action drawer.
- Optional `volume` parameter on `PlayBGM` and `PlaySound` API overloads (defaults to 1).
- Ambient audio: `playOnStart` toggle and a `playClipsRange` to play a random number of
  distinct clips per trigger, plus a `PlayRandom(count)` method that avoids repeating the
  previous trigger's picks.

### Fixed

- Action drawer re-resolves its cached `SerializedProperty` references per element, so
  list entries no longer display or edit the first element's data.

## [1.1.2] - 2026-06-11

### Changed

- Documentation restructured into `Documentation.md` (English) and
  `Documentation.zh-TW.md` (繁體中文), replacing the previous separate Chinese-named
  guides. README rewritten as a bilingual overview.

## [1.1.1] - 2026-06-11

### Changed

- `clipReferenceType` now defaults to `Library` (instead of `File`/unset) for audio
  actions and BGM sequencer steps, so new entries point at the clip library by default.

## [1.1.0] - 2026-06-11

### Added

- `AudioCenterAudioManager.GetTrackOutputVolume(track)` — effective output level for a
  track with mute, master, and (for BGM) duck + fade multipliers folded in, so external
  components can honour player settings.
- Multi-select clip deletion in the audio library editor window.
- `Inspector功能說明.md` — reference for Inspector fields, drawers, custom editors, and menus.

### Changed

- The interactive music mixer now scales by the manager's full effective BGM output
  (mute, duck, fades, master) instead of just music × master.

### Fixed

- `AddComponentMenu` paths now use PascalCase matching the class names
  (e.g. `AudioCenter/AudioCenterAudioManager` instead of `audioCenter/audioCenterAudioManager`).

## [1.0.0] - 2026-06-10

### Added

- Initial package release.
- Core: pooled SFX playback, audio manager, audio controller, sound pool, singleton manager.
- Library: clip assets, clip groups, and a clip library with an editor selector.
- Interactive: ambient audio, interactive music mixer (with demo).
- Sequencer: audio sequencer with BGM, flow, sound, and volume steps.
- Settings: runtime audio settings and a ScriptableObject settings asset.
- Editor: audio editor window, sequencer editor, action drawer, clip library selector.
