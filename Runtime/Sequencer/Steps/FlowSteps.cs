using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AudioCenter
{
    [Serializable]
    public class BgmDuckStep : AudioCenterAudioStep
    {
        [Range(0f, 1f)] public float multiplier = 0.3f;

        public override string StepName => "Flow/BGM Duck";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.SetBgmDuckMultiplier(multiplier);
            yield break;
        }
    }

    [Serializable]
    public class BgmUnduckStep : AudioCenterAudioStep
    {
        public override string StepName => "Flow/BGM Unduck";

        public override IEnumerator Co_Play()
        {
            AudioCenterAudioManager.ClearBgmDuckMultiplier();
            yield break;
        }
    }

    /// <summary>Pauses the sequence for a fixed number of seconds.</summary>
    [Serializable]
    public class WaitStep : AudioCenterAudioStep
    {
        [Min(0f)] public float seconds = 1f;

        public override string StepName => "Flow/Wait";
        public override float Duration => seconds;
        public override bool SelfPaced => true;

        public override IEnumerator Co_Play()
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    /// <summary>
    /// Blocks until the current BGM clip finishes. No-ops immediately if the BGM
    /// is looping or nothing is playing (so it never deadlocks the sequence).
    /// </summary>
    [Serializable]
    public class WaitForBgmCompleteStep : AudioCenterAudioStep
    {
        [Tooltip("Safety cap in seconds (0 = no cap).")]
        [Min(0f)] public float timeout = 0f;

        public override string StepName => "Flow/Wait For BGM";
        public override bool SelfPaced => true;

        public override IEnumerator Co_Play()
        {
            float elapsed = 0f;
            while (AudioCenterAudioManager.IsBgmPlaying() && AudioCenterAudioManager.GetBgmRemainingTime() > 0f)
            {
                if (timeout > 0f && elapsed >= timeout) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    /// <summary>Fires a UnityEvent at this point in the sequence (hook other systems).</summary>
    [Serializable]
    public class UnityEventStep : AudioCenterAudioStep
    {
        public UnityEvent onInvoke;

        public override string StepName => "Flow/Event";

        public override IEnumerator Co_Play()
        {
            onInvoke?.Invoke();
            yield break;
        }
    }
}
