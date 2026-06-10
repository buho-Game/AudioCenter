using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Inspector-friendly audio trigger component.
    ///
    /// Attach to any GameObject (a UI button, a prop, an event host), configure a
    /// <see cref="AudioCenterAudioAction"/> in the Inspector, then call <see cref="DoAction()"/>
    /// from a UnityEvent (Button OnClick), timeline, or other script — or enable
    /// <see cref="actionOnStart"/> to fire automatically on Start.
    ///
    /// This is a plain per-object component — attach as many as you like. The actual
    /// audio engine lives in the <see cref="AudioCenterAudioManager"/> singleton, which is
    /// the global entry point for "play this action from anywhere".
    /// </summary>
    [AddComponentMenu("audioCenter/audioCenterAudioController")]
    public class AudioCenterAudioController : MonoBehaviour
    {
        public bool actionOnStart;
        public AudioCenterAudioAction audioAction;

        private void Start()
        {
            if (actionOnStart)
                DoAction();
        }

        /// <summary>Run this component's serialized <see cref="audioAction"/>.</summary>
        public void DoAction()
        {
            AudioCenterAudioManager.DoAction(audioAction);
        }

        /// <summary>Run an arbitrary action through the audio engine.</summary>
        public void DoAction(AudioCenterAudioAction action)
        {
            AudioCenterAudioManager.DoAction(action);
        }
    }
}
