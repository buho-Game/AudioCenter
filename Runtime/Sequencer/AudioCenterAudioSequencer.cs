using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Feedback-style, audio-only sequencer. A designer assembles an ordered list of
    /// <see cref="AudioCenterAudioStep"/>s in the Inspector and plays the whole sequence with
    /// <see cref="PlaySequence"/> — no scripting required.
    ///
    /// Each step has an <c>initialDelay</c> and a <c>waitForCompletion</c> toggle, so the
    /// same list can mix sequential moments (fade out, wait, then play) with layered
    /// one-shots (fire several SFX at once).
    /// </summary>
    [AddComponentMenu("audioCenter/audioCenterAudioSequencer")]
    public class AudioCenterAudioSequencer : MonoBehaviour
    {
        [Tooltip("Play the sequence automatically on Start.")]
        public bool playOnStart = false;

        [Tooltip("Restart the sequence from the top when it finishes.")]
        public bool loop = false;

        [SerializeReference]
        public List<AudioCenterAudioStep> steps = new List<AudioCenterAudioStep>();

        private Coroutine _runner;
        private readonly List<Coroutine> _spawned = new List<Coroutine>();

        public bool IsPlaying => _runner != null;

        /// <summary>Index of the step currently being processed, or -1 when idle. Used by the editor to highlight it.</summary>
        public int CurrentStepIndex { get; private set; } = -1;

        // When each step last executed (unscaled time), keyed by step index. Lets the
        // editor briefly glow even fire-and-forget steps that run within a single frame.
        private readonly Dictionary<int, float> _lastRunTime = new Dictionary<int, float>();

        /// <summary>Seconds since the step at <paramref name="index"/> last executed, or -1 if it hasn't this run.</summary>
        public float TimeSinceStep(int index)
            => _lastRunTime.TryGetValue(index, out float t) ? Time.unscaledTime - t : -1f;

        private void Start()
        {
            if (playOnStart) PlaySequence();
        }

        private void OnDisable() => StopSequence();

        /// <summary>Starts (or restarts) the sequence from the top.</summary>
        public void PlaySequence()
        {
            StopSequence();
            _lastRunTime.Clear();
            _runner = StartCoroutine(Co_Run());
        }

        /// <summary>
        /// Stops the sequence and any fire-and-forget step coroutines it spawned.
        /// Already-issued manager calls (a playing clip, an in-flight volume fade) are
        /// not reverted here — use explicit Stop / Fade steps for that.
        /// </summary>
        public void StopSequence()
        {
            if (_runner != null)
            {
                StopCoroutine(_runner);
                _runner = null;
            }
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) StopCoroutine(_spawned[i]);
            _spawned.Clear();
            CurrentStepIndex = -1;
        }

        private IEnumerator Co_Run()
        {
            do
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    AudioCenterAudioStep step = steps[i];
                    if (step == null || !step.active) continue;

                    CurrentStepIndex = i;
                    _lastRunTime[i] = Time.unscaledTime;

                    if (step.initialDelay > 0f)
                        yield return new WaitForSeconds(step.initialDelay);

                    if (step.SelfPaced)
                    {
                        // The step paces itself (e.g. Wait / WaitForBgmComplete).
                        yield return StartCoroutine(step.Co_Play());
                    }
                    else
                    {
                        Coroutine c = StartCoroutine(step.Co_Play());
                        if (c != null) _spawned.Add(c);
                        if (step.waitForCompletion && step.Duration > 0f)
                            yield return new WaitForSeconds(step.Duration);
                    }
                }

                // Fire-and-forget handles from this pass are done with once we loop;
                // clearing keeps the list bounded for looping sequences.
                _spawned.Clear();
            }
            while (loop);

            _runner = null;
            CurrentStepIndex = -1;
        }
    }
}
