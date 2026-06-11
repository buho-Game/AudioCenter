using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// AudioCenterAmbientAudio — plays audio from a random pool at random intervals.
    /// Merges RepeatAudioPlayer + RandomPlay into a single component.
    ///
    /// Assign a pool of AudioCenterAudioActions and call DoAction() to start looping,
    /// StopAction() to stop. Each action is routed through the persistent
    /// AudioCenterAudioManager singleton.
    /// </summary>
    [AddComponentMenu("AudioCenter/AudioCenterAmbientAudio")]
    public class AudioCenterAmbientAudio : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private List<AudioCenterAudioAction> audioPool;

        [Header("Interval")]
        [SerializeField] private Vector2 repeatInterval = new Vector2(3f, 8f);

        private bool isPlaying;
        private Coroutine loopCoroutine;

        private void Start()
        {
            // Optionally auto-start; keep opt-in for now
        }

        public void DoAction()
        {
            if (isPlaying) return;
            isPlaying = true;
            loopCoroutine = StartCoroutine(PlayLoop());
        }

        public void StopAction()
        {
            isPlaying = false;
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
                loopCoroutine = null;
            }
        }

        /// <summary>Manually trigger one random audio from the pool.</summary>
        public void PlayRandom()
        {
            if (audioPool == null || audioPool.Count == 0) return;
            int index = Random.Range(0, audioPool.Count);
            AudioCenterAudioManager.DoAction(audioPool[index]);
        }

        private IEnumerator PlayLoop()
        {
            while (isPlaying)
            {
                float waitTime = Random.Range(repeatInterval.x, repeatInterval.y);
                yield return new WaitForSeconds(waitTime);

                if (isPlaying)
                    PlayRandom();
            }
        }
    }
}
