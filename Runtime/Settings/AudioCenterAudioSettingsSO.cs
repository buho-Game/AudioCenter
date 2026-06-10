using UnityEngine;
using UnityEngine.Serialization;

namespace AudioCenter
{
    [CreateAssetMenu(menuName = "AudioCenter/Audio/Game Settings")]
    public class AudioCenterAudioSettingsSO : ScriptableObject
    {
        [Header("Default Volumes")]
        [Range(0f, 1f)] public float defaultMasterVolume = 1f;
        [Range(0f, 1f)] public float defaultMusicVolume = 0.8f;
        [Range(0f, 1f)] public float defaultSfxVolume = 1f;
        [Range(0f, 1f)] public float defaultUIVolume = 1f;

        [Header("Initial State")]
        public bool startMuted = false;

        [Header("Sound Pool")]
        [FormerlySerializedAs("mmPoolSize")] public int poolSize = 10;
        [FormerlySerializedAs("mmPoolCanExpand")] public bool poolCanExpand = true;
    }
}
