using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    /// <summary>
    /// A named group of audio clips embedded directly inside a AudioCenterClipLibrary asset.
    /// Plain serializable class — not a ScriptableObject — so the whole library lives
    /// in one .asset file with no external references required.
    /// </summary>
    [Serializable]
    public class AudioCenterClipGroup
    {
        [SerializeField] private string groupName;
        [SerializeField] private List<AudioCenterClipAsset> assets = new List<AudioCenterClipAsset>();

        public string GroupName => groupName;
        public int Count => assets.Count;
        public AudioCenterClipAsset this[int index] => assets[index];
        public AudioCenterClipAsset this[string clipName] => assets.Find(a => a.clipName == clipName);
        public int FindIndex(string clipName) => assets.FindIndex(a => a.clipName == clipName);
    }
}
