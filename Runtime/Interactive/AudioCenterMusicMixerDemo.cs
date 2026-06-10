using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// Inspector test driver for <see cref="AudioCenterMusicMixer"/>. Enter Play mode, then
    /// right-click this component's header in the Inspector and pick an entry from the
    /// context menu to play / stop the bed and crossfade between the second-music layers.
    /// All switches keep the same timing because the mixer never restarts the timeline.
    ///
    /// Every action prints a Console log (prefixed "[MusicMixer]") with the resulting
    /// state so you can confirm it works. Toggle <see cref="verbose"/> off to silence them.
    /// </summary>
    [AddComponentMenu("audioCenter/audioCenterMusicMixerDemo")]
    [RequireComponent(typeof(AudioCenterMusicMixer))]
    public class AudioCenterMusicMixerDemo : MonoBehaviour
    {
        [Tooltip("Print a Console log after each context-menu action.")]
        public bool verbose = true;

        private AudioCenterMusicMixer _mixer;
        private AudioCenterMusicMixer Mixer => _mixer != null ? _mixer : (_mixer = GetComponent<AudioCenterMusicMixer>());

        [ContextMenu("▶ Play (bed + start layer)")]
        public void PlayMix()
        {
            Mixer.Play();
            Log("Play");
        }

        [ContextMenu("■ Stop")]
        public void StopMix()
        {
            Mixer.Stop();
            Log("Stop");
        }

        [ContextMenu("Switch ▸ Layer 0")]
        public void SwitchLayer0() => SwitchAndLog(0);

        [ContextMenu("Switch ▸ Layer 1")]
        public void SwitchLayer1() => SwitchAndLog(1);

        [ContextMenu("Switch ▸ Layer 2")]
        public void SwitchLayer2() => SwitchAndLog(2);

        [ContextMenu("Switch ▸ Next layer")]
        public void NextLayer()
        {
            Mixer.Next();
            Log("Next");
        }

        [ContextMenu("Switch ▸ Previous layer")]
        public void PrevLayer()
        {
            Mixer.Previous();
            Log("Previous");
        }

        [ContextMenu("Switch ▸ Main page (bed only, custom volume)")]
        public void SwitchMainPage()
        {
            Mixer.SwitchToMainPage();
            Log("SwitchToMainPage");
        }

        [ContextMenu("Mute layers (bed only)")]
        public void MuteLayers()
        {
            Mixer.MuteLayers();
            Log("MuteLayers");
        }

        private void SwitchAndLog(int index)
        {
            if (index >= Mixer.LayerCount)
            {
                if (verbose)
                    Debug.LogWarning($"[MusicMixer] SwitchTo({index}) ignored — only {Mixer.LayerCount} layer(s) configured.", this);
                return;
            }
            Mixer.SwitchTo(index);
            Log($"SwitchTo({index})");
        }

        // Reports the resulting mixer state: play flag, active layer index + name, layer count.
        private void Log(string action)
        {
            if (!verbose) return;

            int active = Mixer.ActiveLayer;
            string activeName = (active >= 0 && active < Mixer.layers.Count)
                ? Mixer.layers[active].name
                : "(main page / bed only)";

            Debug.Log(
                $"[MusicMixer] {action} → playing={Mixer.IsPlaying}, " +
                $"activeLayer={active} \"{activeName}\", layers={Mixer.LayerCount}, " +
                $"dspTime={AudioSettings.dspTime:F2}",
                this);
        }
    }
}
