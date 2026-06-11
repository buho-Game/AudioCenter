# AudioCenter — Documentation

A self-contained Unity audio system built on plain `AudioSource`: pooled playback,
a clip library, volume buses, a layered music mixer, an audio sequencer, ambient
audio, and editor tooling. All types live in the `AudioCenter` namespace and all
components appear under **Add Component → AudioCenter**.

繁體中文版請見 [Documentation.zh-TW.md](Documentation.zh-TW.md)。

## Quick Start

1. **Create the assets** (right-click in the Project window):
   - **Create → AudioCenter → Audio → Clip Library** — holds your named clips
   - **Create → AudioCenter → Audio → Game Settings** — default volumes and pool config
2. **Fill the library** via **AudioCenter → Audio Library** (main menu). Add groups
   (e.g. `BGM`, `SFX`, `UI`) and register clips with a name each.
3. **Add the manager**: put an `AudioCenterAudioManager` component on a GameObject
   in your boot scene and assign the **Library** and **Default Settings** assets.
   The manager persists across scene loads. (The static API auto-creates a manager
   if none exists, but an auto-created one has no library assigned — always place
   one in the scene.)
4. **Play audio** — from code:

```csharp
using AudioCenter;

AudioCenterAudioManager.PlayBGM("BGM", "MainMenu");          // by library lookup
AudioCenterAudioManager.PlaySound("SFX", "Click");           // one-shot SFX
AudioCenterAudioManager.FadeOutBgm(2f);                      // fade BGM to silence
```

   — or without code: add an `AudioCenterAudioController` to any GameObject,
   configure its **Audio Action** in the Inspector, and call `DoAction()` from a
   Button `OnClick` or enable **Action On Start**.

## Core Concepts

### Tracks and buses

Every sound plays on one of three tracks — **BGM**, **SFX**, or **UI**
(`AudioCenterAudioTrack`). Volume and mute are controlled per bus
(`AudioCenterVolumeBus`): **Master**, **BGM**, **SFX**, **UI**. A track's final
output is `bus volume × master volume`, with mutes, BGM ducking, and BGM fades
folded in.

### Clip references: Library vs. File

Anywhere a clip is referenced (audio actions, sequencer steps) you choose a
`AudioCenterClipReferenceType`:

- **Library** (default) — look up by group name + clip name in the manager's
  `AudioCenterClipLibrary`. Recommended: rename or swap clips in one place
  without touching scenes.
- **File** — a direct `AudioClip` reference.

### Pooled playback

The manager plays everything through an internal `AudioSource` pool. Pool size
and whether it can grow are set on the `AudioCenterAudioSettingsSO` asset
(**Pool Size**, default 10; **Pool Can Expand**, default on).

## Scripting API — `AudioCenterAudioManager`

All methods are static. `using AudioCenter;` is assumed.

### BGM

```csharp
PlayBGM(string groupName, string clipName, bool loop = true);
PlayBGM(AudioClip clip, bool loop = true);
StopBGM();
PauseBGM();
ResumeBGM(bool fadeIn = true, float fadeInDuration = 1f);
FadeInBgm(float duration = 1f);      // ramps BGM up from silence
FadeOutBgm(float duration = 1f);     // ramps BGM down to silence
SetBgmDuckMultiplier(float m);       // e.g. 0.3f to duck BGM under a voiceover
ClearBgmDuckMultiplier();
bool  IsBgmPlaying();
float GetBgmTime();                  // playback position in seconds
float GetBgmRemainingTime();         // 0 if looping or nothing playing
```

Playing a new BGM stops the previous one. Fades and ducking are multipliers on
top of the stored music volume — they never overwrite the player's settings.

### SFX / UI sounds

```csharp
AudioSource PlaySound(string groupName, string clipName,
    bool loop = false,
    AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
    bool rndPitch = false, Vector2 rndRange = default);

AudioSource PlaySound(string groupName, string clipName, AudioCenterAudioTrack track,
    bool loop = false, AudioCenterPlaySoundMode playMode = ..., bool rndPitch = false, Vector2 rndRange = default);

AudioSource PlaySound(AudioClip clip, ...);                              // same optional args
AudioSource PlaySound(AudioClip clip, AudioCenterAudioTrack track, ...); // same optional args

StopSound(AudioClip clip);
StopAllSound();
```

- `AudioCenterPlaySoundMode` — what to do if the same clip is already playing:
  `ReplayIfExisted` (restart it), `IgnoreIfExisted` (keep the existing one),
  `AllowMultiple` (overlap).
- `rndPitch` / `rndRange` — randomize pitch per play as `1f + Random.Range(x, y)`,
  e.g. `new Vector2(-0.3f, 0.3f)` for natural variation on repeated sounds.
- The default track is `SFX`; pass `AudioCenterAudioTrack.UI` for UI sounds.

### Volume, mute, fades

```csharp
float GetBusVolume(AudioCenterVolumeBus bus);
SetBusVolume(AudioCenterVolumeBus bus, float volume);        // instant; cancels a running fade on that bus
SetBusMute(AudioCenterVolumeBus bus, bool mute);
FadeVolume(AudioCenterVolumeBus bus, float target, float duration);   // unscaled time
FadeMasterVolume(float target, float duration);
FadeTrackVolume(AudioCenterAudioTrack track, float target, float duration);
CancelAllFades();                                            // stops fades, keeps current volumes
float GetTrackOutputVolume(AudioCenterAudioTrack track);     // instance method; effective output incl. mute/duck/fade

// Convenience setters used by settings UIs (clamped 0..1):
UpdateMasterVolume(float v);  UpdateMusicVolume(float v);
UpdateSfxVolume(float v);     UpdateUIVolume(float v);

// Read-only properties (instance): MasterVolume, MusicVolume, SfxVolume, UIVolume
```

### Settings persistence

```csharp
AudioCenterAudioData data = AudioCenterAudioManager.GetAudioSettings();
// serialize `data` into your save file, then on load:
AudioCenterAudioManager.SetAudioSettings(data);
```

`AudioCenterAudioData` holds `masterVolume`, `musicVolume`, `sfxVolume`,
`uiVolume`, `isMuteBgm`, `isMuteSound`, `isMuteMaster`.

### Actions

```csharp
AudioCenterAudioManager.DoAction(AudioCenterAudioAction action);
```

`AudioCenterAudioAction` is the serializable descriptor used by
`AudioCenterAudioController` and `AudioCenterAmbientAudio`: source type
(BGM/Sound), action (play/stop/pause/resume/fade for BGM; play/stop/stop-all/
attach-on-BGM for sounds), clip reference, track, loop, play mode, and pitch
randomization. **AttachOnBGM** starts a sound at the BGM's current playback
position, so a stem authored against the current track stays beat-aligned.

## Components

### AudioCenterAudioController — per-object trigger

Attach to any GameObject and configure one **Audio Action** in the Inspector.

| Field | Meaning |
|---|---|
| Action On Start | Fire the action automatically on `Start()` |
| Audio Action | The serialized action (dynamic fields, see Editor Tools) |

Methods: `DoAction()` runs the configured action; `DoAction(AudioCenterAudioAction)`
runs an arbitrary one. Both are UnityEvent-compatible — wire a Button's
`OnClick` to `DoAction()` for click sounds.

### AudioCenterAmbientAudio — random environmental sounds

Plays a random action from a pool at random intervals (bird chirps, creaks, wind).

| Field | Meaning |
|---|---|
| Audio Pool | List of `AudioCenterAudioAction` candidates |
| Repeat Interval | Min/max seconds between plays (default 3–8) |

Methods: `DoAction()` starts the loop, `StopAction()` stops it, `PlayRandom()`
fires one random sound immediately. It does not auto-start.

### AudioCenterAudioSettings — volume slider binding

Attach to a settings panel and assign the **Music / Sfx / Ui Volume Slider**
references. On `Start()` it syncs the sliders to the current settings and
subscribes to their `onValueChanged` — no manual event wiring needed.
`UpdateMusicVolume/UpdateSfxVolume/UpdateUIVolume(float)` are also public for
UnityEvent use.

### AudioCenterMusicMixer — layered (vertical) music

One **Main Bgm** bed loops continuously while switchable **Layers** ride on top.
All sources are scheduled to the same DSP start time and loop, so they stay
sample-accurately phase-locked — switching layers only crossfades gain, never
restarts the timeline. The mixer plays its own sources (not the pool) but scales
them by the manager's BGM output, so player volume settings still apply.

| Field | Meaning |
|---|---|
| Main Bgm | The always-playing base clip |
| Bed Volume | Bed level while a layer is active |
| Bed Only Volume | Bed level in the "main page" state (no layer) |
| Layers | List of `{ name, clip, volume }` stems |
| Start Layer | Layer made audible on `Play()` (−1 = bed only) |
| Play On Start | Start automatically (default on) |
| Crossfade Duration | Seconds to crossfade on switch (0 = hard cut) |
| Schedule Lead Time | DSP scheduling lead (default 0.1 s) |

```csharp
Play();  Stop();
SwitchTo(int index);            // -1 = bed only
SwitchTo(int index, bool instant);
SwitchTo(string layerName);
Next();  Previous();
MuteLayers();                   // fade out all layers, keep bed
SwitchToMainPage();             // bed only, at Bed Only Volume
// Properties: IsPlaying, ActiveLayer, LayerCount
```

> **Constraint:** all clips must be stems of the same loop — same length and
> tempo as the bed. Clips of differing length drift across loop boundaries.

`AudioCenterMusicMixerDemo` (requires a mixer on the same object) adds context-menu
test entries — in Play mode, right-click the component header to play/stop and
switch layers, with state logs prefixed `[MusicMixer]` (toggle **Verbose**).

### AudioCenterAudioSequencer — step-list choreography

An ordered list of audio steps played with `PlaySequence()` — no scripting needed.

| Field | Meaning |
|---|---|
| Play On Start | Run the sequence on `Start()` |
| Loop | Restart from the top when finished |
| Steps | The step list (custom inspector, see Editor Tools) |

Step types (the **+** dropdown groups them):

- **BGM** — Play, Stop, Pause, Resume, Fade In, Fade Out
- **Sound** — Play, Stop, Stop All
- **Volume** — Set, Mute, Fade (per bus)
- **Flow** — Wait, Wait For BGM (blocks until the current non-looping BGM ends,
  optional timeout), BGM Duck, BGM Unduck, Event (fires a `UnityEvent`)

Every step has: **Label** (display name), **Active** (untick to skip),
**Initial Delay** (seconds before firing), and **Wait For Completion** (hold the
sequence for the step's duration — e.g. a fade — before the next step;
fire-and-forget when off).

```csharp
PlaySequence();  StopSequence();
// Properties: IsPlaying, CurrentStepIndex (-1 when idle)
float TimeSinceStep(int index);   // -1 if the step hasn't run this pass
```

`StopSequence()` halts the sequence but does not revert already-issued calls
(a playing clip, an in-flight fade) — add explicit Stop/Fade steps for that.

## Assets

### AudioCenterClipLibrary

**Create → AudioCenter → Audio → Clip Library.** One `.asset` containing named
groups, each holding `{ clipName, clip }` entries:

```
ClipLibrary
├─ "BGM":  "MainMenu" → MainPage.mp3, "Battle" → Battle.mp3, …
├─ "SFX":  "Click" → Click.wav, …
└─ "UI":   …
```

Runtime access: `library["SFX"]` → `AudioCenterClipGroup`; `group["Click"]` →
`AudioCenterClipAsset` (`clipName`, `clip`); plus `GroupCount`, `Count`,
`FindIndex(name)`, and `int` indexers.

### AudioCenterAudioSettingsSO

**Create → AudioCenter → Audio → Game Settings.** Read once by the manager on
`Awake`:

| Field | Default | Meaning |
|---|---|---|
| Default Master/Music/Sfx/UI Volume | 1 / 0.8 / 1 / 1 | Initial bus volumes |
| Start Muted | off | Start with all buses muted |
| Pool Size | 10 | Pre-allocated `AudioSource` count |
| Pool Can Expand | on | Grow the pool when exhausted |

## Editor Tools

- **Audio Library window** (**AudioCenter → Audio Library**) — two-pane editor:
  group list on the left, the selected group's clips on the right. Toolbar picks
  which library asset to edit; **+ Group** adds groups; per-group panel renames,
  deletes, and manages clip rows (multi-select delete supported).
- **Audio Action drawer** — `AudioCenterAudioAction` fields cascade in the
  Inspector: choose BGM or Sound → choose the action → choose File or Library
  reference → Library shows Group/Clip dropdowns sourced from the project's
  library asset. Only relevant fields are shown.
- **Sequencer inspector** — reorderable step list (drag the handle), **+**
  dropdown grouped by category, expand/collapse per step. In Play mode the
  executing step highlights green and Play/Stop buttons appear at the bottom.

## FAQ

**No sound plays.** Check, in order: an `AudioCenterAudioManager` exists in the
scene with the **Library** asset assigned; the group/clip names match the
library exactly (case-sensitive); no bus is muted and volumes are above 0; for
library references, the clip slot in the library actually has a clip.

**How do I loop a sound?** Pass `loop: true` to `PlaySound`/`PlayBGM`, or tick
**Loop** on the action/step. Stop a looping sound with `StopSound(clip)` or a
Sound/Stop step.

**Repeated sounds feel robotic.** Enable **Rnd Pitch** with a range like
(−0.3, 0.3) so each play varies slightly.

**Library or File reference?** Library — it is the default and keeps clip
assignments in one asset. Use File only for one-off clips that don't belong in
the library.

**Music layers drift out of sync.** Ensure every layer clip has exactly the same
length and tempo as the Main Bgm bed; they must be stems of the same loop.
