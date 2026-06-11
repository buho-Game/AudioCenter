using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Vertical (layered) music mixer: one main BGM bed plays continuously while a
    /// set of switchable "second music" layers ride on top of it. Every layer — bed
    /// included — is scheduled to the same DSP start time and loops, so they stay
    /// sample-accurately phase-locked. Switching layers only crossfades their gain;
    /// the audio timeline never restarts, so the new layer comes in on the same beat
    /// as the bed ("same timing").
    ///
    /// For perfect alignment all clips should share the bed's length and tempo
    /// (i.e. they are stems of the same loop). Clips of differing length will drift
    /// across loop boundaries.
    ///
    /// The "main page" state (no layer active, only the bed) is itself selectable
    /// via <see cref="SwitchToMainPage"/> and gets its own <see cref="bedOnlyVolume"/>,
    /// so the bed can sit at a different level when it plays alone than when a layer
    /// rides on top of it.
    ///
    /// These sources play directly (not through the AudioCenterAudioManager pool) so they
    /// can be DSP-scheduled, but their gain is scaled by <see cref="AudioCenterAudioManager"/>'s music
    /// and master volume so the mixer still respects the player's settings.
    /// </summary>
    [AddComponentMenu("audioCenter/audioCenterMusicMixer")]
    public class AudioCenterMusicMixer : MonoBehaviour
    {
        [Serializable]
        public class MusicLayer
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Main bed (always playing under the layers)")]
        public AudioClip mainBgm;
        [Tooltip("Bed volume while a layer is active.")]
        [Range(0f, 1f)] public float bedVolume = 1f;

        [Tooltip("Bed volume in the \"main page\" state — bed only, no layer active.")]
        [Range(0f, 1f)] public float bedOnlyVolume = 1f;

        [Header("Switchable second-music layers")]
        [Tooltip("All layers should share the bed's length & tempo so they stay phase-locked.")]
        public List<MusicLayer> layers = new List<MusicLayer>();

        [Tooltip("Layer made audible on Play (-1 = bed only, no layer).")]
        public int startLayer = 0;

        [Header("Behaviour")]
        public bool playOnStart = true;

        [Tooltip("Seconds to crossfade gain when switching layers (0 = hard cut).")]
        [Min(0f)] public float crossfadeDuration = 0.75f;

        [Tooltip("Lead time before the scheduled start so every source begins together.")]
        [Min(0.02f)] public float scheduleLeadTime = 0.1f;

        // ── Runtime ──────────────────────────────────────────────────────────────
        private AudioSource _bedSource;
        private readonly List<AudioSource> _layerSources = new List<AudioSource>();
        private readonly List<float> _weight = new List<float>();       // current gain weight
        private readonly List<float> _targetWeight = new List<float>(); // crossfade target
        private float _bedWeight;        // current bed gain (crossfaded)
        private float _bedTargetWeight;  // bed crossfade target
        private int _activeLayer = -1;
        private bool _playing;
        private AudioCenterAudioManager _manager;

        public int ActiveLayer => _activeLayer;
        public bool IsPlaying => _playing;
        public int LayerCount => layers.Count;

        private void Awake() => _manager = FindObjectOfType<AudioCenterAudioManager>();

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void OnDisable() => Stop();

        /// <summary>Schedules the bed and every layer from a common DSP time and loops them.</summary>
        public void Play()
        {
            Stop();
            if (mainBgm == null && layers.Count == 0) return;

            double startDsp = AudioSettings.dspTime + Mathf.Max(0.02f, scheduleLeadTime);

            if (mainBgm != null)
            {
                _bedSource = CreateSource("Bed", mainBgm);
                _bedSource.PlayScheduled(startDsp);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                string label = string.IsNullOrEmpty(layers[i].name) ? "Layer_" + i : layers[i].name;
                AudioSource src = CreateSource(label, layers[i].clip);
                _layerSources.Add(src);
                _weight.Add(0f);
                _targetWeight.Add(0f);
                src?.PlayScheduled(startDsp);
            }

            _playing = true;
            _activeLayer = -1;
            // Start in the "main page" state: bed only, at its custom volume.
            _bedTargetWeight = bedOnlyVolume;
            _bedWeight = bedOnlyVolume;
            if (_bedSource != null)
                _bedSource.volume = _bedWeight * ManagerScale();
            if (startLayer >= 0 && startLayer < layers.Count)
                SwitchTo(startLayer, instant: true);
        }

        /// <summary>Stops everything and tears down the runtime sources.</summary>
        public void Stop()
        {
            if (_bedSource != null) Destroy(_bedSource.gameObject);
            _bedSource = null;
            for (int i = 0; i < _layerSources.Count; i++)
                if (_layerSources[i] != null) Destroy(_layerSources[i].gameObject);
            _layerSources.Clear();
            _weight.Clear();
            _targetWeight.Clear();
            _bedWeight = 0f;
            _bedTargetWeight = 0f;
            _activeLayer = -1;
            _playing = false;
        }

        /// <summary>Crossfades to the layer at <paramref name="index"/> (-1 = bed only).</summary>
        public void SwitchTo(int index) => SwitchTo(index, false);

        public void SwitchTo(int index, bool instant)
        {
            if (!_playing) Play();
            for (int i = 0; i < _targetWeight.Count; i++)
                _targetWeight[i] = (i == index) ? Mathf.Clamp01(layers[i].volume) : 0f;
            _activeLayer = (index >= 0 && index < layers.Count) ? index : -1;

            // Bed rides at bedOnlyVolume in the main-page (no-layer) state,
            // and at bedVolume whenever a layer is active.
            _bedTargetWeight = (_activeLayer < 0) ? bedOnlyVolume : bedVolume;

            if (instant)
            {
                for (int i = 0; i < _weight.Count; i++)
                    _weight[i] = _targetWeight[i];
                _bedWeight = _bedTargetWeight;
            }
        }

        /// <summary>Crossfades to the first layer whose name matches.</summary>
        public void SwitchTo(string layerName)
        {
            int idx = layers.FindIndex(l => l.name == layerName);
            if (idx >= 0) SwitchTo(idx);
        }

        public void Next()
        {
            if (layers.Count == 0) return;
            SwitchTo((Mathf.Max(_activeLayer, 0) + 1) % layers.Count);
        }

        public void Previous()
        {
            if (layers.Count == 0) return;
            int from = Mathf.Max(_activeLayer, 0);
            SwitchTo((from - 1 + layers.Count) % layers.Count);
        }

        /// <summary>Fades all layers out, leaving only the bed.</summary>
        public void MuteLayers() => SwitchTo(-1);

        /// <summary>
        /// Switches to the "main page" state: all layers fade out and only the bed
        /// plays, crossfading to its custom <see cref="bedOnlyVolume"/>.
        /// </summary>
        public void SwitchToMainPage() => SwitchTo(-1);

        private void Update()
        {
            if (!_playing) return;

            float scale = ManagerScale();
            float step = crossfadeDuration > 0f ? Time.unscaledDeltaTime / crossfadeDuration : 1f;

            if (_bedSource != null)
            {
                _bedWeight = Mathf.MoveTowards(_bedWeight, _bedTargetWeight, step);
                _bedSource.volume = _bedWeight * scale;
            }

            for (int i = 0; i < _layerSources.Count; i++)
            {
                _weight[i] = Mathf.MoveTowards(_weight[i], _targetWeight[i], step);
                if (_layerSources[i] != null)
                    _layerSources[i].volume = _weight[i] * scale;
            }
        }

        private AudioSource CreateSource(string label, AudioClip clip)
        {
            if (clip == null) return null;
            var go = new GameObject("[MusicMixer] " + label);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // pure 2D music
            src.volume = 0f;
            return src;
        }

        // Scale by the manager's effective BGM output (mute, duck, fades and master
        // folded in) so the mixer honours player settings like any pooled BGM voice.
        private float ManagerScale()
        {
            if (_manager == null) _manager = FindObjectOfType<AudioCenterAudioManager>();
            if (_manager == null) return 1f;
            return Mathf.Clamp01(_manager.GetTrackOutputVolume(AudioCenterAudioTrack.BGM));
        }
    }
}
