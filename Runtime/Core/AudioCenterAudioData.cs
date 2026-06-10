using System;

namespace AudioCenter
{
    [Serializable]
    public class AudioCenterAudioData
    {
        public bool isMuteBgm;
        public bool isMuteSound;
        public bool isMuteMaster;
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float uiVolume;

        public AudioCenterAudioData(
            bool isMuteBgm = false,
            bool isMuteSound = false,
            float musicVolume = 0.8f,
            float sfxVolume = 1f,
            float uiVolume = 1f,
            float masterVolume = 1f,
            bool isMuteMaster = false)
        {
            this.isMuteBgm = isMuteBgm;
            this.isMuteSound = isMuteSound;
            this.isMuteMaster = isMuteMaster;
            this.masterVolume = masterVolume;
            this.musicVolume = musicVolume;
            this.sfxVolume = sfxVolume;
            this.uiVolume = uiVolume;
        }
    }
}
