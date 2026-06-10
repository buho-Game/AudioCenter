using System;
using System.Collections;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Base class for a single action inside a <see cref="AudioCenterAudioSequencer"/>.
    ///
    /// Steps are stored polymorphically via [SerializeReference], so a designer can
    /// mix any concrete step type in one ordered list. Most steps just fire a call on
    /// <see cref="AudioCenterAudioManager"/> and return immediately; the sequencer is what
    /// blocks for <see cref="Duration"/> when <see cref="waitForCompletion"/> is on.
    ///
    /// Convention:
    ///  - Fire-and-forget steps (Play / Set / Mute / Duck / Fade) do the work in
    ///    <see cref="Co_Play"/> and report a <see cref="Duration"/> so the runner can
    ///    optionally wait for them.
    ///  - Self-pacing steps (Wait / WaitForBgmComplete) yield internally inside
    ///    <see cref="Co_Play"/> and report Duration => 0 to avoid double-waiting.
    /// </summary>
    [Serializable]
    public abstract class AudioCenterAudioStep
    {
        [Tooltip("Optional designer label shown in the sequencer list header.")]
        public string label;

        [Tooltip("Disabled steps are skipped by the sequencer.")]
        public bool active = true;

        [Min(0f)]
        [Tooltip("Seconds to wait before this step fires.")]
        public float initialDelay = 0f;

        [Tooltip("When on, the sequencer waits for this step's Duration before starting the next step.")]
        public bool waitForCompletion = false;

        /// <summary>Menu/inspector display name. Use a "Group/Name" path for grouping in the add menu.</summary>
        public virtual string StepName => GetType().Name;

        /// <summary>How long this step occupies the timeline when <see cref="waitForCompletion"/> is on.</summary>
        public virtual float Duration => 0f;

        /// <summary>
        /// When true the sequencer always yields on this step's own coroutine
        /// (regardless of <see cref="waitForCompletion"/>), because the step paces itself.
        /// </summary>
        public virtual bool SelfPaced => false;

        /// <summary>Performs the step's work. Fire-and-return unless <see cref="SelfPaced"/>.</summary>
        public abstract IEnumerator Co_Play();
    }
}
