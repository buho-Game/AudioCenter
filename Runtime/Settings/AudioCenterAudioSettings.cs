using UnityEngine;
using UnityEngine.UI;

namespace AudioCenter
{
    /// <summary>
    /// AudioCenterAudioSettings — UI component for volume sliders.
    /// Attach to a settings panel and wire each slider to the corresponding
    /// UnityEvent callback in the Inspector, or use Initialize() at runtime.
    /// </summary>
    [AddComponentMenu("AudioCenter/AudioCenterAudioSettings")]
    public class AudioCenterAudioSettings : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;

        private void Start()
        {
            SyncSlidersToSettings();

            musicVolumeSlider?.onValueChanged.AddListener(UpdateMusicVolume);
            sfxVolumeSlider?.onValueChanged.AddListener(UpdateSfxVolume);
            uiVolumeSlider?.onValueChanged.AddListener(UpdateUIVolume);
        }

        private void OnDestroy()
        {
            musicVolumeSlider?.onValueChanged.RemoveListener(UpdateMusicVolume);
            sfxVolumeSlider?.onValueChanged.RemoveListener(UpdateSfxVolume);
            uiVolumeSlider?.onValueChanged.RemoveListener(UpdateUIVolume);
        }

        private void SyncSlidersToSettings()
        {
            AudioCenterAudioData data = AudioCenterAudioManager.GetAudioSettings();
            if (data == null) return;

            if (musicVolumeSlider != null) musicVolumeSlider.value = data.musicVolume;
            if (sfxVolumeSlider   != null) sfxVolumeSlider.value   = data.sfxVolume;
            if (uiVolumeSlider    != null) uiVolumeSlider.value    = data.uiVolume;
        }

        // ── Callbacks (also callable from Inspector UnityEvents) ─────────────

        public void UpdateMusicVolume(float volume) => AudioCenterAudioManager.UpdateMusicVolume(volume);
        public void UpdateSfxVolume(float volume)   => AudioCenterAudioManager.UpdateSfxVolume(volume);
        public void UpdateUIVolume(float volume)    => AudioCenterAudioManager.UpdateUIVolume(volume);
    }
}
