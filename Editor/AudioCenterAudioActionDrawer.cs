using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace AudioCenter.Editor
{
    [CustomPropertyDrawer(typeof(AudioCenterAudioAction))]
    public class AudioCenterAudioActionDrawer : PropertyDrawer
    {
        private const int spacing = 20;
        private const string emptyString = "----------";

        // Drawer will look for a AudioCenterClipLibrary asset via AssetDatabase
        private AudioCenterClipLibrary clipLibrary;
        private bool initialized = false;

        private SerializedProperty type;
        private SerializedProperty bgmActionType;
        private SerializedProperty soundActionType;
        private SerializedProperty loop;
        private SerializedProperty clipReferenceType;
        private SerializedProperty groupName;
        private SerializedProperty clipName;
        private SerializedProperty clip;
        private SerializedProperty soundMode;
        private SerializedProperty bgmResumeFadeIn;
        private SerializedProperty fadeDuration;
        private SerializedProperty rndPitch;
        private SerializedProperty rndRange;
        private SerializedProperty track;

        private Rect backgroundRect;
        private Rect currentPosition;
        private Color rectColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        private void Initialize(SerializedProperty property)
        {
            if (initialized) return;

            LoadLibrary();

            type             = property.FindPropertyRelative("type");
            bgmActionType    = property.FindPropertyRelative("bgmActionType");
            soundActionType  = property.FindPropertyRelative("soundActionType");
            loop             = property.FindPropertyRelative("loop");
            clipReferenceType = property.FindPropertyRelative("clipReferenceType");
            groupName        = property.FindPropertyRelative("groupName");
            clipName         = property.FindPropertyRelative("clipName");
            clip             = property.FindPropertyRelative("clip");
            soundMode        = property.FindPropertyRelative("soundMode");
            bgmResumeFadeIn  = property.FindPropertyRelative("bgmResumeFadeIn");
            fadeDuration     = property.FindPropertyRelative("fadeDuration");
            rndPitch         = property.FindPropertyRelative("rndPitch");
            rndRange         = property.FindPropertyRelative("rndRange");
            track            = property.FindPropertyRelative("track");

            initialized = true;
        }

        private void LoadLibrary()
        {
            // Find any AudioCenterClipLibrary asset in the project
            string[] guids = AssetDatabase.FindAssets("t:AudioCenterClipLibrary");
            if (guids.Length > 0)
                clipLibrary = AssetDatabase.LoadAssetAtPath<AudioCenterClipLibrary>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);

            position.y += spacing * 0.5f;
            backgroundRect = new Rect(position.position + new Vector2(-4, 0), position.size + new Vector2(8, -10));
            EditorGUI.DrawRect(backgroundRect, rectColor);

            currentPosition = position;
            currentPosition.height = spacing;
            currentPosition.y += 4;
            DrawField(currentPosition, type, "Type");

            currentPosition.y += spacing;

            switch ((AudioCenterAudioSourceType)type.enumValueIndex)
            {
                case AudioCenterAudioSourceType.BGM:
                    DrawField(currentPosition, bgmActionType, "Action");
                    DrawBGMFields();
                    break;

                case AudioCenterAudioSourceType.Sound:
                    DrawField(currentPosition, soundActionType, "Action");
                    currentPosition.y += spacing;
                    DrawField(currentPosition, track, "Track");
                    DrawSoundFields();
                    break;
            }
        }

        private void DrawBGMFields()
        {
            switch ((AudioCenterBgmActionType)bgmActionType.enumValueIndex)
            {
                case AudioCenterBgmActionType.Play:
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, clipReferenceType, "Reference");
                    DrawClipField();
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, loop, "Loop");
                    break;

                case AudioCenterBgmActionType.Resume:
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, bgmResumeFadeIn, "Fade In");
                    if (bgmResumeFadeIn.boolValue)
                    {
                        currentPosition.y += spacing;
                        DrawField(currentPosition, fadeDuration, "Fade Duration");
                    }
                    break;

                case AudioCenterBgmActionType.FadeIn:
                case AudioCenterBgmActionType.FadeOut:
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, fadeDuration, "Fade Duration");
                    break;
            }
        }

        private void DrawSoundFields()
        {
            switch ((AudioCenterSoundActionType)soundActionType.enumValueIndex)
            {
                case AudioCenterSoundActionType.Play:
                case AudioCenterSoundActionType.AttachOnBGM:
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, clipReferenceType, "Reference");
                    DrawClipField();
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, loop, "Loop");
                    currentPosition.y += spacing;
                    DrawField(currentPosition, soundMode, "Play Mode");
                    currentPosition.y += spacing;
                    DrawField(currentPosition, rndPitch, "Random Pitch");
                    if (rndPitch.boolValue)
                    {
                        currentPosition.y += spacing;
                        DrawField(currentPosition, rndRange, "Pitch Range");
                    }
                    break;

                case AudioCenterSoundActionType.Stop:
                    currentPosition.y += spacing * 1.5f;
                    DrawField(currentPosition, clip, "Clip");
                    break;
            }
        }

        private void DrawClipField()
        {
            if ((AudioCenterClipReferenceType)clipReferenceType.enumValueIndex == AudioCenterClipReferenceType.File)
            {
                currentPosition.y += spacing;
                DrawField(currentPosition, clip, "Clip");
            }
            else
            {
                DrawLibrarySelector();
            }
        }

        private void DrawLibrarySelector()
        {
            if (clipLibrary == null)
            {
                currentPosition.y += spacing;
                EditorGUI.HelpBox(currentPosition, "No AudioCenterClipLibrary found in project.", MessageType.Warning);
                return;
            }

            // Group popup
            int groupPopupIndex = 0;
            int gIndex = clipLibrary.FindIndex(groupName.stringValue);
            if (gIndex != -1) groupPopupIndex = gIndex + 1;

            currentPosition.y += spacing;
            int newGroupIdx = EditorGUI.Popup(currentPosition, "Group", groupPopupIndex, BuildGroupOptions().ToArray());
            groupName.stringValue = newGroupIdx > 0 ? clipLibrary[newGroupIdx - 1].GroupName : "";

            // Clip popup
            AudioCenterClipGroup group = clipLibrary[groupName.stringValue];
            currentPosition.y += spacing;
            if (group == null)
            {
                EditorGUI.Popup(currentPosition, "Clip", 0, new[] { emptyString });
                clipName.stringValue = "";
            }
            else
            {
                int clipPopupIndex = 0;
                int cIndex = group.FindIndex(clipName.stringValue);
                if (cIndex != -1) clipPopupIndex = cIndex + 1;

                int newClipIdx = EditorGUI.Popup(currentPosition, "Clip", clipPopupIndex, BuildClipOptions(group).ToArray());
                clipName.stringValue = newClipIdx > 0 ? group[newClipIdx - 1].clipName : "";
            }
        }

        private IEnumerable<string> BuildGroupOptions()
        {
            yield return emptyString;
            for (int i = 0; i < clipLibrary.GroupCount; i++)
                yield return clipLibrary[i].GroupName;
        }

        private IEnumerable<string> BuildClipOptions(AudioCenterClipGroup group)
        {
            yield return emptyString;
            for (int i = 0; i < group.Count; i++)
                yield return group[i].clipName;
        }

        private void DrawField(Rect rect, SerializedProperty prop, string label)
        {
            Rect content = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            EditorGUI.PropertyField(content, prop, GUIContent.none);
        }

        private int propertyHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            propertyHeight = 16 + spacing * 2;

            switch ((AudioCenterAudioSourceType)type.enumValueIndex)
            {
                case AudioCenterAudioSourceType.BGM:
                    switch ((AudioCenterBgmActionType)bgmActionType.enumValueIndex)
                    {
                        case AudioCenterBgmActionType.Play:
                            propertyHeight += spacing * 3;
                            propertyHeight += (AudioCenterClipReferenceType)clipReferenceType.enumValueIndex == AudioCenterClipReferenceType.File
                                ? spacing : spacing * 2;
                            break;
                        case AudioCenterBgmActionType.Resume:
                            propertyHeight += (int)(spacing * 1.5f);
                            if (bgmResumeFadeIn.boolValue) propertyHeight += spacing;
                            break;
                        case AudioCenterBgmActionType.FadeIn:
                        case AudioCenterBgmActionType.FadeOut:
                            propertyHeight += (int)(spacing * 1.5f);
                            break;
                    }
                    break;

                case AudioCenterAudioSourceType.Sound:
                    propertyHeight += spacing; // track field
                    switch ((AudioCenterSoundActionType)soundActionType.enumValueIndex)
                    {
                        case AudioCenterSoundActionType.Play:
                        case AudioCenterSoundActionType.AttachOnBGM:
                            propertyHeight += spacing * 5;
                            propertyHeight += (AudioCenterClipReferenceType)clipReferenceType.enumValueIndex == AudioCenterClipReferenceType.File
                                ? spacing : spacing * 2;
                            if (rndPitch.boolValue) propertyHeight += spacing;
                            break;
                        case AudioCenterSoundActionType.Stop:
                            propertyHeight += (int)(spacing * 1.5f);
                            break;
                    }
                    break;
            }
            return propertyHeight;
        }
    }
}
