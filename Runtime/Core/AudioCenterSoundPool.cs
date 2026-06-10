using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// AudioCenterSoundPool — the native audio backend behind <see cref="AudioCenterAudioManager"/>.
    ///
    /// Maintains a pool of plain 2D <see cref="AudioSource"/>s (one per "voice").
    /// Each playing voice remembers its <see cref="AudioCenterAudioTrack"/> and a base
    /// volume; the source's actual volume is always base × the track's output
    /// factor, which the manager supplies through <see cref="_trackVolume"/>. When
    /// a bus volume or mute changes the manager calls <see cref="RefreshVolumes"/>
    /// to re-apply the factor to every live voice.
    ///
    /// Self-contained: no third-party dependency, no scene setup, no AudioMixer
    /// asset.
    /// </summary>
    public class AudioCenterSoundPool
    {
        private class Voice
        {
            public AudioSource source;
            public AudioCenterAudioTrack track;
            public float baseVolume;
            public bool active;
        }

        private readonly Transform _parent;
        private readonly bool _canExpand;
        private readonly Func<AudioCenterAudioTrack, float> _trackVolume;
        private readonly List<Voice> _voices = new List<Voice>();

        public AudioCenterSoundPool(
            Transform parent,
            int initialSize,
            bool canExpand,
            Func<AudioCenterAudioTrack, float> trackVolumeResolver)
        {
            _parent = parent;
            _canExpand = canExpand;
            _trackVolume = trackVolumeResolver;

            for (int i = 0; i < Mathf.Max(1, initialSize); i++)
                CreateVoice();
        }

        // ── Playback ──────────────────────────────────────────────────────────

        /// <summary>
        /// Plays <paramref name="clip"/> on a free voice. Volume is
        /// <paramref name="baseVolume"/> × the track's current output factor.
        /// <paramref name="startTime"/> offsets the playback head (seconds) — used
        /// to start a voice in phase with another track; it is wrapped into the
        /// clip length so an offset past the end still lands on a valid position.
        /// Returns the AudioSource so the caller can track / stop it.
        /// </summary>
        public AudioSource Play(
            AudioClip clip,
            AudioCenterAudioTrack track,
            bool loop,
            float baseVolume,
            float pitch,
            float startTime = 0f)
        {
            if (clip == null) return null;

            Voice v = GetFreeVoice();
            if (v == null) return null;

            v.track = track;
            v.baseVolume = baseVolume;
            v.active = true;

            AudioSource src = v.source;
            src.clip = clip;
            src.loop = loop;
            src.pitch = pitch;
            src.volume = baseVolume * _trackVolume(track);
            if (startTime > 0f && clip.length > 0f)
                src.time = Mathf.Repeat(startTime, clip.length);
            src.Play();
            return src;
        }

        public void Stop(AudioSource source)
        {
            Voice v = Find(source);
            if (v == null) return;
            v.source.Stop();
            v.source.clip = null;
            v.active = false;
        }

        public void Pause(AudioSource source) => source?.Pause();

        public void Resume(AudioSource source) => source?.UnPause();

        /// <summary>First playing voice whose clip matches, or null.</summary>
        public AudioSource FindByClip(AudioClip clip)
        {
            if (clip == null) return null;
            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (v.active && v.source.clip == clip)
                    return v.source;
            }
            return null;
        }

        public void StopAll()
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (!v.active) continue;
                v.source.Stop();
                v.source.clip = null;
                v.active = false;
            }
        }

        /// <summary>
        /// Re-applies every live voice's volume from the current track factors.
        /// Called by the manager whenever a bus volume / mute / duck / fade changes.
        /// </summary>
        public void RefreshVolumes()
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (v.active && v.source != null)
                    v.source.volume = v.baseVolume * _trackVolume(v.track);
            }
        }

        // ── Pool management ───────────────────────────────────────────────────

        // Reclaims any voice whose non-looping clip has finished, then returns a
        // free voice (expanding the pool if allowed).
        private Voice GetFreeVoice()
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (v.active && !v.source.isPlaying && !v.source.loop)
                {
                    v.active = false;
                    v.source.clip = null;
                }
                if (!v.active)
                    return v;
            }

            return _canExpand ? CreateVoice() : null;
        }

        private Voice CreateVoice()
        {
            var go = new GameObject("[AudioCenterSound] Voice " + _voices.Count);
            go.transform.SetParent(_parent, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // pure 2D
            src.volume = 0f;

            var v = new Voice { source = src, active = false };
            _voices.Add(v);
            return v;
        }

        private Voice Find(AudioSource source)
        {
            if (source == null) return null;
            for (int i = 0; i < _voices.Count; i++)
                if (_voices[i].source == source)
                    return _voices[i];
            return null;
        }
    }
}
