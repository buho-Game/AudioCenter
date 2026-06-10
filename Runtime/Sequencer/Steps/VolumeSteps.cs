using System;
using System.Collections;
using UnityEngine;

namespace AudioCenter
{
    [Serializable]
    public class VolumeSetStep : AudioCenterAudioStep
    {
        public AudioCenterVolumeBus bus = AudioCenterVolumeBus.Master;
        [Range(0f, 1f)] public float volume = 1f;

        public override string StepName => "Volume/Set";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.SetBusVolume(bus, volume);
            yield break;
        }
    }

    [Serializable]
    public class MuteStep : AudioCenterAudioStep
    {
        public AudioCenterVolumeBus bus = AudioCenterVolumeBus.Master;
        public bool mute = true;

        public override string StepName => "Volume/Mute";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.SetBusMute(bus, mute);
            yield break;
        }
    }

    [Serializable]
    public class VolumeFadeStep : AudioCenterAudioStep
    {
        public AudioCenterVolumeBus bus = AudioCenterVolumeBus.Master;
        [Range(0f, 1f)] public float target = 0f;
        [Min(0f)] public float fadeDuration = 1f;

        public override string StepName => "Volume/Fade";
        public override float Duration => fadeDuration;

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.FadeVolume(bus, target, fadeDuration);
            yield break;
        }
    }
}
