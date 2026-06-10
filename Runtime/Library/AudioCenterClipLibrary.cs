using System.Collections.Generic;
using UnityEngine;

namespace AudioCenter
{
    [CreateAssetMenu(menuName = "AudioCenter/Audio/Clip Library")]
    public class AudioCenterClipLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioCenterClipGroup> groups = new List<AudioCenterClipGroup>();

        public int GroupCount => groups.Count;
        public AudioCenterClipGroup this[int index] => groups[index];
        public AudioCenterClipGroup this[string groupName] => groups.Find(g => g.GroupName == groupName);
        public int FindIndex(string groupName) => groups.FindIndex(g => g.GroupName == groupName);
    }
}
