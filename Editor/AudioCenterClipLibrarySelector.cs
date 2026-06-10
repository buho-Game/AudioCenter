using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AudioCenter.Editor
{
    /// <summary>
    /// Shared editor helper that draws AudioCenterClipLibrary "Group" + "Clip" popups for a
    /// pair of string SerializedProperties. Extracted so both the legacy
    /// AudioCenterAudioActionDrawer and the new sequencer step drawers can reuse it.
    /// </summary>
    public static class AudioCenterClipLibrarySelector
    {
        private const string EmptyOption = "----------";
        private static AudioCenterClipLibrary _cached;

        public static AudioCenterClipLibrary Library
        {
            get
            {
                if (_cached == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:AudioCenterClipLibrary");
                    if (guids.Length > 0)
                        _cached = AssetDatabase.LoadAssetAtPath<AudioCenterClipLibrary>(
                            AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                return _cached;
            }
        }

        /// <summary>Height needed to draw the selector (group + clip lines, or a warning box).</summary>
        public static float GetHeight()
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return Library == null ? line : line * 2f;
        }

        /// <summary>Draws the Group and Clip popups, writing the chosen names back into the properties.</summary>
        public static void Draw(Rect rect, SerializedProperty groupName, SerializedProperty clipName)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float step = lineH + EditorGUIUtility.standardVerticalSpacing;

            AudioCenterClipLibrary lib = Library;
            if (lib == null)
            {
                EditorGUI.HelpBox(new Rect(rect.x, rect.y, rect.width, lineH),
                    "No AudioCenterClipLibrary found in project.", MessageType.Warning);
                return;
            }

            // Group popup
            int groupIndex = 0;
            int gIdx = lib.FindIndex(groupName.stringValue);
            if (gIdx != -1) groupIndex = gIdx + 1;

            Rect groupRect = new Rect(rect.x, rect.y, rect.width, lineH);
            int newGroup = EditorGUI.Popup(groupRect, "Group", groupIndex, BuildGroupOptions(lib));
            groupName.stringValue = newGroup > 0 ? lib[newGroup - 1].GroupName : "";

            // Clip popup
            Rect clipRect = new Rect(rect.x, rect.y + step, rect.width, lineH);
            AudioCenterClipGroup group = lib[groupName.stringValue];
            if (group == null)
            {
                EditorGUI.Popup(clipRect, "Clip", 0, new[] { EmptyOption });
                clipName.stringValue = "";
            }
            else
            {
                int clipIndex = 0;
                int cIdx = group.FindIndex(clipName.stringValue);
                if (cIdx != -1) clipIndex = cIdx + 1;

                int newClip = EditorGUI.Popup(clipRect, "Clip", clipIndex, BuildClipOptions(group));
                clipName.stringValue = newClip > 0 ? group[newClip - 1].clipName : "";
            }
        }

        private static string[] BuildGroupOptions(AudioCenterClipLibrary lib)
        {
            var list = new List<string> { EmptyOption };
            for (int i = 0; i < lib.GroupCount; i++) list.Add(lib[i].GroupName);
            return list.ToArray();
        }

        private static string[] BuildClipOptions(AudioCenterClipGroup group)
        {
            var list = new List<string> { EmptyOption };
            for (int i = 0; i < group.Count; i++) list.Add(group[i].clipName);
            return list.ToArray();
        }
    }
}
