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

        [Header("Playback")]
        [SerializeField] private bool playOnStart = false;

        [Tooltip("How many clips from the pool are played on each trigger (min..max, inclusive).")]
        [SerializeField] private Vector2Int playClipsRange = new Vector2Int(1, 1);

        [Header("Interval")]
        [SerializeField] private Vector2 repeatInterval = new Vector2(3f, 8f);

        private bool isPlaying;
        private Coroutine loopCoroutine;
        private readonly List<int> pickBuffer = new List<int>();
        private readonly List<int> lastPicks = new List<int>();

        private void OnValidate()
        {
            playClipsRange.x = Mathf.Max(1, playClipsRange.x);
            playClipsRange.y = Mathf.Max(playClipsRange.x, playClipsRange.y);
        }

        private void Start()
        {
            if (playOnStart)
                DoAction();
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

        /// <summary>
        /// Manually trigger random audio from the pool. Plays a random number
        /// of distinct clips between playClipsRange.x and playClipsRange.y.
        /// </summary>
        public void PlayRandom()
        {
            int count = Random.Range(playClipsRange.x, playClipsRange.y + 1);
            PlayRandom(count);
        }

        /// <summary>
        /// Trigger a specific number of distinct random clips from the pool.
        /// Clips played on the previous trigger are excluded when the pool
        /// has enough other clips to cover the requested count.
        /// </summary>
        public void PlayRandom(int count)
        {
            if (audioPool == null || audioPool.Count == 0) return;
            count = Mathf.Clamp(count, 1, audioPool.Count);

            pickBuffer.Clear();
            for (int i = 0; i < audioPool.Count; i++)
            {
                if (!lastPicks.Contains(i))
                    pickBuffer.Add(i);
            }

            // Not enough unplayed clips left — fall back to the full pool.
            if (pickBuffer.Count < count)
            {
                pickBuffer.Clear();
                for (int i = 0; i < audioPool.Count; i++)
                    pickBuffer.Add(i);
            }

            lastPicks.Clear();
            for (int i = 0; i < count; i++)
            {
                int pick = Random.Range(0, pickBuffer.Count);
                int index = pickBuffer[pick];
                AudioCenterAudioManager.DoAction(audioPool[index]);
                lastPicks.Add(index);
                pickBuffer.RemoveAt(pick);
            }
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
