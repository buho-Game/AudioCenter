using UnityEditor;
using UnityEngine;
using AudioCenter.Editor;

namespace AudioCenter.Editor.UI
{
    /// <summary>
    /// Custom inspector for <see cref="AudioCenter.UI.AudioCenterCustomButton"/>.
    ///
    /// Replaces the raw SFX group/clip text fields with the same Group + Clip popups the
    /// AudioController uses (<see cref="AudioCenterClipLibrarySelector"/>), so the click sound
    /// is picked from the project's AudioCenterClipLibrary instead of being typed by hand.
    /// </summary>
    [CustomEditor(typeof(AudioCenter.UI.AudioCenterCustomButton))]
    [CanEditMultipleObjects]
    public class AudioCenterCustomButtonEditor : UnityEditor.Editor
    {
        private SerializedProperty _animationConfig;
        private SerializedProperty _enableHoverAnimation;
        private SerializedProperty _enableClickAnimation;
        private SerializedProperty _enableSFX;
        private SerializedProperty _sfxGroupName;
        private SerializedProperty _sfxClipName;
        private SerializedProperty _onButtonClicked;

        private void OnEnable()
        {
            _animationConfig = serializedObject.FindProperty("animationConfig");
            _enableHoverAnimation = serializedObject.FindProperty("enableHoverAnimation");
            _enableClickAnimation = serializedObject.FindProperty("enableClickAnimation");
            _enableSFX = serializedObject.FindProperty("enableSFX");
            _sfxGroupName = serializedObject.FindProperty("sfxGroupName");
            _sfxClipName = serializedObject.FindProperty("sfxClipName");
            _onButtonClicked = serializedObject.FindProperty("onButtonClicked");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Animation
            EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animationConfig);
            EditorGUILayout.PropertyField(_enableHoverAnimation);
            EditorGUILayout.PropertyField(_enableClickAnimation);

            EditorGUILayout.Space();

            // SFX
            EditorGUILayout.LabelField("SFX", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableSFX);

            using (new EditorGUI.DisabledScope(!_enableSFX.boolValue))
            {
                // Same Group + Clip popups as the AudioController, driven by the project's
                // AudioCenterClipLibrary. Writes the chosen names back into the string fields.
                Rect rect = EditorGUILayout.GetControlRect(true, AudioCenterClipLibrarySelector.GetHeight());
                AudioCenterClipLibrarySelector.Draw(rect, _sfxGroupName, _sfxClipName);
            }

            EditorGUILayout.Space();

            // Events
            EditorGUILayout.PropertyField(_onButtonClicked);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
