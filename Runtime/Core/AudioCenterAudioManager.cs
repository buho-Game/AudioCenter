using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// AudioCenterAudioManager — the central audio facade for all AudioCenter games.
    ///
    /// Routes BGM / SFX / UI through a native pooled AudioSource backend
    /// (<see cref="AudioCenterSoundPool"/>); per-bus volume and mute are applied by
    /// scaling each playing source. No third-party audio plugin required.
    ///
    /// Usage:
    ///   AudioCenterAudioManager.PlayBGM(clip);
    ///   AudioCenterAudioManager.PlaySound("SFX", "Hit");
    ///   AudioCenterAudioManager.SetBgmDuckMultiplier(0.3f);
    /// </summary>
    [AddComponentMenu("audioCenter/audioCenterAudioManager")]
    public class AudioCenterAudioManager : AudioCenterSingletonManager<AudioCenterAudioManager>
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Library")]
        [SerializeField] private AudioCenterClipLibrary library;

        [Header("Settings")]
        [SerializeField] private AudioCenterAudioSettingsSO defaultSettings;

        // ── State ──────────────────────────────────────────────────────────────

        private AudioCenterAudioData settings;
        private float bgmDuckMultiplier = 1f;

        // Fade multiplier on the BGM track, driven by FadeInBgm/FadeOutBgm.
        // Separate from the stored music volume so fades don't mutate settings.
        private float bgmFadeMultiplier = 1f;
        private Coroutine bgmFadeRoutine;

        // SFX/UI can be muted independently of the shared sound-mute flag.
        private bool sfxMuted;
        private bool uiMuted;

        // The native AudioSource pool that actually plays everything.
        private AudioCenterSoundPool pool;

        // Track the currently playing BGM AudioSource so we can fade/stop it
        private AudioSource activeBGMSource;

        // Active volume-lerp fades, keyed by bus so a new fade cancels the old one
        private readonly Dictionary<AudioCenterVolumeBus, Coroutine> volumeFades =
            new Dictionary<AudioCenterVolumeBus, Coroutine>();

        // ── Properties ────────────────────────────────────────────────────────

        public float MasterVolume => settings.masterVolume;
        public float MusicVolume  => settings.musicVolume;
        public float SfxVolume    => settings.sfxVolume;
        public float UIVolume     => settings.uiVolume;

        private float MasterFactor => settings.isMuteMaster ? 0f : settings.masterVolume;

        /// <summary>
        /// Effective output level for a track with mute, master and (for BGM) the
        /// duck + fade multipliers folded in. For components that play their own
        /// AudioSources (e.g. the music mixer) but must honour player settings.
        /// </summary>
        public float GetTrackOutputVolume(AudioCenterAudioTrack track) => TrackOutputVolume(track);

        // The single source of truth for a track's output level: bus × master,
        // folding in mute and (for BGM) the duck + fade multipliers. The pool
        // multiplies each voice's base volume by this.
        private float TrackOutputVolume(AudioCenterAudioTrack track)
        {
            switch (track)
            {
                case AudioCenterAudioTrack.BGM:
                    return settings.isMuteBgm
                        ? 0f
                        : settings.musicVolume * bgmDuckMultiplier * bgmFadeMultiplier * MasterFactor;
                case AudioCenterAudioTrack.UI:
                    return (settings.isMuteSound || uiMuted) ? 0f : settings.uiVolume * MasterFactor;
                default: // SFX
                    return (settings.isMuteSound || sfxMuted) ? 0f : settings.sfxVolume * MasterFactor;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            settings = defaultSettings != null
                ? new AudioCenterAudioData(
                    musicVolume:  defaultSettings.defaultMusicVolume,
                    sfxVolume:    defaultSettings.defaultSfxVolume,
                    uiVolume:     defaultSettings.defaultUIVolume,
                    masterVolume: defaultSettings.defaultMasterVolume,
                    isMuteBgm:    defaultSettings.startMuted,
                    isMuteSound:  defaultSettings.startMuted,
                    isMuteMaster: defaultSettings.startMuted)
                : new AudioCenterAudioData();

            int poolSize = defaultSettings != null ? defaultSettings.poolSize : 10;
            bool poolCanExpand = defaultSettings == null || defaultSettings.poolCanExpand;
            pool = new AudioCenterSoundPool(transform, poolSize, poolCanExpand, TrackOutputVolume);

            // Drop any fades left over from a previous play session
            // (relevant when "Enter Play Mode (no domain reload)" is enabled).
            CancelAllFades();

            ApplyVolumes();
        }

        // ── Settings API ──────────────────────────────────────────────────────

        public static AudioCenterAudioData GetAudioSettings() => Instance.settings;

        public static void SetAudioSettings(AudioCenterAudioData data)
        {
            Instance.settings = data;
            Instance.ApplyVolumes();
        }

        public static void UpdateMusicVolume(float volume)
        {
            Instance.settings.musicVolume = Mathf.Clamp01(volume);
            Instance.ApplyVolumes();
        }

        public static void UpdateSfxVolume(float volume)
        {
            Instance.settings.sfxVolume = Mathf.Clamp01(volume);
            Instance.ApplyVolumes();
        }

        public static void UpdateUIVolume(float volume)
        {
            Instance.settings.uiVolume = Mathf.Clamp01(volume);
            Instance.ApplyVolumes();
        }

        public static void UpdateMasterVolume(float volume)
        {
            Instance.settings.masterVolume = Mathf.Clamp01(volume);
            Instance.ApplyVolumes();
        }

        // ── Bus Volume / Mute API ─────────────────────────────────────────────

        /// <summary>Reads the stored normalized volume (0..1) for a bus.</summary>
        public static float GetBusVolume(AudioCenterVolumeBus bus)
        {
            AudioCenterAudioData s = Instance.settings;
            switch (bus)
            {
                case AudioCenterVolumeBus.Master: return s.masterVolume;
                case AudioCenterVolumeBus.BGM:    return s.musicVolume;
                case AudioCenterVolumeBus.SFX:    return s.sfxVolume;
                case AudioCenterVolumeBus.UI:     return s.uiVolume;
                default:                   return 1f;
            }
        }

        /// <summary>
        /// Sets a bus volume instantly. Cancels any in-flight fade on that bus
        /// so an explicit set always wins.
        /// </summary>
        public static void SetBusVolume(AudioCenterVolumeBus bus, float volume)
        {
            Instance.CancelFade(bus);
            ApplyBusVolume(bus, volume);
        }

        // Writes a bus volume without touching running fades (used by the fade loop).
        private static void ApplyBusVolume(AudioCenterVolumeBus bus, float volume)
        {
            switch (bus)
            {
                case AudioCenterVolumeBus.Master: UpdateMasterVolume(volume); break;
                case AudioCenterVolumeBus.BGM:    UpdateMusicVolume(volume);  break;
                case AudioCenterVolumeBus.SFX:    UpdateSfxVolume(volume);    break;
                case AudioCenterVolumeBus.UI:     UpdateUIVolume(volume);     break;
            }
        }

        /// <summary>
        /// Mutes/unmutes a bus. Master/BGM route through the persistent settings
        /// model; SFX/UI use their own per-track mute flags so they can be
        /// toggled independently without disturbing the shared sound-mute flag.
        /// </summary>
        public static void SetBusMute(AudioCenterVolumeBus bus, bool mute)
        {
            switch (bus)
            {
                case AudioCenterVolumeBus.Master:
                    Instance.settings.isMuteMaster = mute;
                    break;
                case AudioCenterVolumeBus.BGM:
                    Instance.settings.isMuteBgm = mute;
                    break;
                case AudioCenterVolumeBus.SFX:
                    Instance.sfxMuted = mute;
                    break;
                case AudioCenterVolumeBus.UI:
                    Instance.uiMuted = mute;
                    break;
            }
            Instance.ApplyVolumes();
        }

        // ── Volume Fade API ───────────────────────────────────────────────────

        /// <summary>
        /// Lerps a bus volume to <paramref name="target"/> over <paramref name="duration"/>
        /// seconds (unscaled time). A new fade on the same bus cancels the previous one.
        /// </summary>
        public static void FadeVolume(AudioCenterVolumeBus bus, float target, float duration)
        {
            AudioCenterAudioManager inst = Instance;
            inst.CancelFade(bus);

            target = Mathf.Clamp01(target);
            if (duration <= 0f)
            {
                ApplyBusVolume(bus, target);
                return;
            }
            inst.volumeFades[bus] = inst.StartCoroutine(inst.Co_FadeVolume(bus, target, duration));
        }

        public static void FadeMasterVolume(float target, float duration)
            => FadeVolume(AudioCenterVolumeBus.Master, target, duration);

        public static void FadeTrackVolume(AudioCenterAudioTrack track, float target, float duration)
            => FadeVolume(TrackToBus(track), target, duration);

        /// <summary>Stops every running volume fade (does not change current volumes).</summary>
        public static void CancelAllFades()
        {
            AudioCenterAudioManager inst = Instance;
            foreach (Coroutine c in inst.volumeFades.Values)
                if (c != null) inst.StopCoroutine(c);
            inst.volumeFades.Clear();
        }

        private void CancelFade(AudioCenterVolumeBus bus)
        {
            if (volumeFades.TryGetValue(bus, out Coroutine c))
            {
                if (c != null) StopCoroutine(c);
                volumeFades.Remove(bus);
            }
        }

        private IEnumerator Co_FadeVolume(AudioCenterVolumeBus bus, float target, float duration)
        {
            float start = GetBusVolume(bus);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                ApplyBusVolume(bus, Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            ApplyBusVolume(bus, target);
            volumeFades.Remove(bus);
        }

        private static AudioCenterVolumeBus TrackToBus(AudioCenterAudioTrack track)
        {
            switch (track)
            {
                case AudioCenterAudioTrack.BGM: return AudioCenterVolumeBus.BGM;
                case AudioCenterAudioTrack.UI:  return AudioCenterVolumeBus.UI;
                default:                 return AudioCenterVolumeBus.SFX;
            }
        }

        // ── BGM Query Helpers (for sequencer wait steps) ──────────────────────

        public static bool IsBgmPlaying()
            => Instance.activeBGMSource != null && Instance.activeBGMSource.isPlaying;

        /// <summary>Current playback position of the BGM clip in seconds, or 0 if none.</summary>
        public static float GetBgmTime()
            => Instance.activeBGMSource != null ? Instance.activeBGMSource.time : 0f;

        /// <summary>Seconds left on the current BGM clip, or 0 if none / looping.</summary>
        public static float GetBgmRemainingTime()
        {
            AudioSource src = Instance.activeBGMSource;
            if (src == null || src.clip == null || !src.isPlaying || src.loop) return 0f;
            return Mathf.Max(0f, src.clip.length - src.time);
        }

        // ── BGM API ───────────────────────────────────────────────────────────

        public static void PlayBGM(string groupName, string clipName, bool loop = true)
        {
            if (Instance.library == null) return;
            AudioCenterClipGroup group = Instance.library[groupName];
            if (group == null) return;
            AudioCenterClipAsset asset = group[clipName];
            if (asset?.clip == null) return;
            Instance.InternalPlayBGM(asset.clip, loop);
        }

        public static void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            Instance.InternalPlayBGM(clip, loop);
        }

        public static void StopBGM()
        {
            if (Instance.activeBGMSource != null)
                Instance.pool.Stop(Instance.activeBGMSource);
        }

        public static void PauseBGM()
        {
            if (Instance.activeBGMSource != null)
                Instance.pool.Pause(Instance.activeBGMSource);
        }

        public static void ResumeBGM(bool fadeIn = true, float fadeInDuration = 1f)
        {
            if (Instance.activeBGMSource == null) return;
            Instance.pool.Resume(Instance.activeBGMSource);
            if (fadeIn && fadeInDuration > 0f)
                FadeInBgm(fadeInDuration);
        }

        public static void FadeInBgm(float duration = 1f)
        {
            if (Instance.activeBGMSource == null || duration <= 0f) return;
            Instance.bgmFadeMultiplier = 0f; // ramp up from silence
            Instance.StartBgmFade(1f, duration);
        }

        public static void FadeOutBgm(float duration = 1f)
        {
            if (Instance.activeBGMSource == null || duration <= 0f) return;
            Instance.StartBgmFade(0f, duration);
        }

        // Lerps bgmFadeMultiplier to target over duration (unscaled), refreshing
        // live volumes each step so the BGM track ramps in/out.
        private void StartBgmFade(float target, float duration)
        {
            if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = StartCoroutine(Co_FadeBgm(target, duration));
        }

        private IEnumerator Co_FadeBgm(float target, float duration)
        {
            float start = bgmFadeMultiplier;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                bgmFadeMultiplier = Mathf.Lerp(start, target, t / duration);
                pool?.RefreshVolumes();
                yield return null;
            }
            bgmFadeMultiplier = target;
            pool?.RefreshVolumes();
            bgmFadeRoutine = null;
        }

        public static void SetBgmDuckMultiplier(float multiplier)
        {
            Instance.bgmDuckMultiplier = Mathf.Clamp01(multiplier);
            Instance.ApplyVolumes();
        }

        public static void ClearBgmDuckMultiplier()
        {
            Instance.bgmDuckMultiplier = 1f;
            Instance.ApplyVolumes();
        }

        // ── SFX API ───────────────────────────────────────────────────────────

        public static AudioSource PlaySound(
            string groupName,
            string clipName,
            bool loop = false,
            AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
            bool rndPitch = false,
            Vector2 rndRange = new Vector2())
            => PlaySound(groupName, clipName, AudioCenterAudioTrack.SFX, loop, playMode, rndPitch, rndRange);

        public static AudioSource PlaySound(
            string groupName,
            string clipName,
            AudioCenterAudioTrack track,
            bool loop = false,
            AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
            bool rndPitch = false,
            Vector2 rndRange = new Vector2())
        {
            if (Instance.library == null) return null;
            AudioCenterClipGroup group = Instance.library[groupName];
            if (group == null) return null;
            AudioCenterClipAsset asset = group[clipName];
            if (asset?.clip == null) return null;
            return Instance.InternalPlaySound(asset.clip, track, loop, playMode, rndPitch, rndRange);
        }

        public static AudioSource PlaySound(
            AudioClip clip,
            bool loop = false,
            AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
            bool rndPitch = false,
            Vector2 rndRange = new Vector2())
            => PlaySound(clip, AudioCenterAudioTrack.SFX, loop, playMode, rndPitch, rndRange);

        public static AudioSource PlaySound(
            AudioClip clip,
            AudioCenterAudioTrack track,
            bool loop = false,
            AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
            bool rndPitch = false,
            Vector2 rndRange = new Vector2())
        {
            if (clip == null) return null;
            return Instance.InternalPlaySound(clip, track, loop, playMode, rndPitch, rndRange);
        }

        public static void StopSound(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource found = Instance.pool.FindByClip(clip);
            if (found != null)
                Instance.pool.Stop(found);
        }

        public static void StopAllSound()
        {
            Instance.pool.StopAll();
        }

        // ── Action API ────────────────────────────────────────────────────────

        /// <summary>
        /// Run a serialized <see cref="AudioCenterAudioAction"/> — the global entry point
        /// behind AudioCenterAudioController and AudioCenterAmbientAudio. Routes by source type
        /// to the matching BGM/Sound API above.
        /// </summary>
        public static void DoAction(AudioCenterAudioAction action)
        {
            if (action == null)
                return;

            switch (action.type)
            {
                case AudioCenterAudioSourceType.BGM:
                    ExecuteBGMAction(action);
                    break;
                case AudioCenterAudioSourceType.Sound:
                    ExecuteSoundAction(action);
                    break;
            }
        }

        private static void ExecuteBGMAction(AudioCenterAudioAction action)
        {
            switch (action.bgmActionType)
            {
                case AudioCenterBgmActionType.Play:
                    if (action.clipReferenceType == AudioCenterClipReferenceType.File)
                        PlayBGM(action.clip, action.loop);
                    else
                        PlayBGM(action.groupName, action.clipName, action.loop);
                    break;

                case AudioCenterBgmActionType.Stop:
                    StopBGM();
                    break;

                case AudioCenterBgmActionType.Pause:
                    PauseBGM();
                    break;

                case AudioCenterBgmActionType.Resume:
                    ResumeBGM(action.bgmResumeFadeIn, action.fadeDuration);
                    break;

                case AudioCenterBgmActionType.FadeIn:
                    FadeInBgm(action.fadeDuration);
                    break;

                case AudioCenterBgmActionType.FadeOut:
                    FadeOutBgm(action.fadeDuration);
                    break;
            }
        }

        private static void ExecuteSoundAction(AudioCenterAudioAction action)
        {
            switch (action.soundActionType)
            {
                case AudioCenterSoundActionType.Play:
                    if (action.clipReferenceType == AudioCenterClipReferenceType.File)
                        PlaySound(
                            action.clip,
                            action.track,
                            action.loop,
                            action.soundMode,
                            action.rndPitch,
                            action.rndRange);
                    else
                        PlaySound(
                            action.groupName,
                            action.clipName,
                            action.track,
                            action.loop,
                            action.soundMode,
                            action.rndPitch,
                            action.rndRange);
                    break;

                // Start the sound in phase with the BGM playback head, so a clip
                // authored as a stem of the current track stays beat-aligned.
                case AudioCenterSoundActionType.AttachOnBGM:
                {
                    AudioClip clip = ResolveActionClip(action);
                    if (clip != null)
                        Instance.InternalPlaySound(
                            clip,
                            action.track,
                            action.loop,
                            action.soundMode,
                            action.rndPitch,
                            action.rndRange,
                            GetBgmTime());
                    break;
                }

                case AudioCenterSoundActionType.Stop:
                    if (action.clip != null)
                        StopSound(action.clip);
                    break;

                case AudioCenterSoundActionType.StopAll:
                    StopAllSound();
                    break;
            }
        }

        // Resolves the clip an action points at, whether by direct File reference
        // or by group/name lookup in the library. Returns null if unresolved.
        private static AudioClip ResolveActionClip(AudioCenterAudioAction action)
        {
            if (action.clipReferenceType == AudioCenterClipReferenceType.File)
                return action.clip;

            if (Instance.library == null) return null;
            AudioCenterClipGroup group = Instance.library[action.groupName];
            AudioCenterClipAsset asset = group?[action.clipName];
            return asset?.clip;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void InternalPlayBGM(AudioClip clip, bool loop)
        {
            if (pool == null) return;

            // Stop previous BGM
            if (activeBGMSource != null)
                pool.Stop(activeBGMSource);

            // A fresh BGM plays at full fade; FadeInBgm overrides this if called.
            bgmFadeMultiplier = 1f;

            activeBGMSource = pool.Play(clip, AudioCenterAudioTrack.BGM, loop, baseVolume: 1f, pitch: 1f);
        }

        private AudioSource InternalPlaySound(
            AudioClip clip,
            AudioCenterAudioTrack track,
            bool loop,
            AudioCenterPlaySoundMode playMode,
            bool rndPitch,
            Vector2 rndRange,
            float startTime = 0f)
        {
            if (pool == null) return null;

            if (playMode == AudioCenterPlaySoundMode.IgnoreIfExisted)
            {
                AudioSource existing = pool.FindByClip(clip);
                if (existing != null && existing.isPlaying) return existing;
            }
            else if (playMode == AudioCenterPlaySoundMode.ReplayIfExisted)
            {
                AudioSource existing = pool.FindByClip(clip);
                if (existing != null) pool.Stop(existing);
            }

            float pitch = rndPitch ? 1f + Random.Range(rndRange.x, rndRange.y) : 1f;

            // Bus volume + mute are folded into TrackOutputVolume, so the base
            // volume is simply 1 — SFX and UI route to their own tracks.
            return pool.Play(clip, track, loop, baseVolume: 1f, pitch: pitch, startTime: startTime);
        }

        // Re-applies the current bus volumes / mutes to every live source.
        private void ApplyVolumes() => pool?.RefreshVolumes();
    }
}
