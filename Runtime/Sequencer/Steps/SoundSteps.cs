using System;
using System.Collections;
using UnityEngine;

namespace AudioCenter
{
    [Serializable]
    public class SoundPlayStep : AudioCenterClipStep
    {
        [Tooltip("Which non-music track to play on.")]
        public AudioCenterAudioTrack track = AudioCenterAudioTrack.SFX;
        public bool loop = false;
        public AudioCenterPlaySoundMode soundMode = AudioCenterPlaySoundMode.ReplayIfExisted;
        public bool rndPitch = false;
        public Vector2 rndRange = new Vector2(-0.3f, 0.3f);

        public override string StepName => "Sound/Play";
        public override float Duration => loop ? 0f : FileClipDuration;

        public override IEnumerator Co_Play()
        {
            if (clipReferenceType == AudioCenterClipReferenceType.File)
                AudioCenterAudioManager.PlaySound(clip, track, loop, soundMode, rndPitch, rndRange);
            else
                AudioCenterAudioManager.PlaySound(groupName, clipName, track, loop, soundMode, rndPitch, rndRange);
            yield break;
        }
    }

    [Serializable]
    public class SoundStopStep : AudioCenterAudioStep
    {
        public AudioClip clip;

        public override string StepName => "Sound/Stop";

        public override IEnumerator Co_Play()
        {
            if (clip != null) AudioCenterAudioManager.StopSound(clip);
            yield break;
        }
    }

    [Serializable]
    public class SoundStopAllStep : AudioCenterAudioStep
    {
        public override string StepName => "Sound/Stop All";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.StopAllSound();
            yield break;
        }
    }
}
