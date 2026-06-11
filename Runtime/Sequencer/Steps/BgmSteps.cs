using System;
using System.Collections;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Shared base for steps that reference a clip either directly (File) or by
    /// library group/clip name (Library). Field names match the legacy
    /// <see cref="AudioCenterAudioAction"/> so editor selectors can be reused.
    /// </summary>
    [Serializable]
    public abstract class AudioCenterClipStep : AudioCenterAudioStep
    {
        public AudioCenterClipReferenceType clipReferenceType = AudioCenterClipReferenceType.Library;
        public string groupName;
        public string clipName;
        public AudioClip clip;

        /// <summary>Clip length when a File clip is assigned, otherwise 0 (unknown for Library refs).</summary>
        protected float FileClipDuration =>
            (clipReferenceType == AudioCenterClipReferenceType.File && clip != null) ? clip.length : 0f;
    }

    // ── BGM ───────────────────────────────────────────────────────────────────

    [Serializable]
    public class BgmPlayStep : AudioCenterClipStep
    {
        public bool loop = true;

        public override string StepName => "BGM/Play";

        public override IEnumerator Co_Play()
        {
            if (clipReferenceType == AudioCenterClipReferenceType.File)
                AudioCenterAudioManager.PlayBGM(clip, loop);
            else
                AudioCenterAudioManager.PlayBGM(groupName, clipName, loop);
            yield break;
        }
    }

    [Serializable]
    public class BgmStopStep : AudioCenterAudioStep
    {
        public override string StepName => "BGM/Stop";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.StopBGM();
            yield break;
        }
    }

    [Serializable]
    public class BgmPauseStep : AudioCenterAudioStep
    {
        public override string StepName => "BGM/Pause";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.PauseBGM();
            yield break;
        }
    }

    [Serializable]
    public class BgmResumeStep : AudioCenterAudioStep
    {
        public bool fadeIn = true;
        [Min(0f)] public float fadeDuration = 1f;

        public override string StepName => "BGM/Resume";
        public override float Duration => fadeIn ? fadeDuration : 0f;

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.ResumeBGM(fadeIn, fadeDuration);
            yield break;
        }
    }

    [Serializable]
    public class BgmFadeInStep : AudioCenterAudioStep
    {
        [Min(0f)] public float fadeDuration = 1f;

        public override string StepName => "BGM/Fade In";
        public override float Duration => fadeDuration;

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.FadeInBgm(fadeDuration);
            yield break;
        }
    }

    [Serializable]
    public class BgmFadeOutStep : AudioCenterAudioStep
    {
        [Min(0f)] public float fadeDuration = 1f;

        public override string StepName => "BGM/Fade Out";
        public override float Duration => fadeDuration;

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.FadeOutBgm(fadeDuration);
            yield break;
        }
    }
}
