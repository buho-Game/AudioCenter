using System;
using UnityEngine;

namespace AudioCenter
{
    public enum AudioCenterAudioSourceType
    {
        BGM,
        Sound
    }

    public enum AudioCenterAudioTrack
    {
        BGM,
        SFX,
        UI
    }

    /// <summary>
    /// Volume/mute bus targets, including the Master bus that scales every track.
    /// </summary>
    public enum AudioCenterVolumeBus
    {
        Master,
        BGM,
        SFX,
        UI
    }

    public enum AudioCenterBgmActionType
    {
        Play,
        Stop,
        Pause,
        Resume,
        FadeIn,
        FadeOut
    }

    public enum AudioCenterSoundActionType
    {
        Play,
        Stop,
        StopAll,
        AttachOnBGM
    }

    public enum AudioCenterClipReferenceType
    {
        File,
        Library
    }

    public enum AudioCenterPlaySoundMode
    {
        ReplayIfExisted,
        IgnoreIfExisted,
        AllowMultiple
    }

    [Serializable]
    public class AudioCenterAudioAction
    {
        public AudioCenterAudioSourceType type;

        // BGM
        public AudioCenterBgmActionType bgmActionType;
        public bool bgmResumeFadeIn = true;
        [Range(0f, 10f)] public float fadeDuration = 1f;

        // Sound
        public AudioCenterSoundActionType soundActionType;
        public AudioCenterAudioTrack track = AudioCenterAudioTrack.SFX;
        public AudioCenterPlaySoundMode soundMode = AudioCenterPlaySoundMode.ReplayIfExisted;
        public bool rndPitch = false;
        public Vector2 rndRange = new Vector2(-0.3f, 0.3f);

        // Clip reference
        public AudioCenterClipReferenceType clipReferenceType = AudioCenterClipReferenceType.Library;
        public string groupName;
        public string clipName;
        public AudioClip clip;
        public bool loop;

        // Per-action multiplier on top of the bus/settings volume.
        [Range(0f, 1f)] public float volume = 1f;
    }
}
