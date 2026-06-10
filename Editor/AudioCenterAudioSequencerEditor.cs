using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace AudioCenter.Editor
{
    [CustomEditor(typeof(AudioCenterAudioSequencer))]
    public class AudioCenterAudioSequencerEditor : UnityEditor.Editor
    {
        private SerializedProperty _playOnStart;
        private SerializedProperty _loop;
        private SerializedProperty _steps;
        private ReorderableList _list;

        // Left column reserved for the reorderable list's drag handle, so it doesn't
        // overlap the per-step foldout arrow. Reorder by dragging this handle.
        private const float HandleColumnWidth = 14f;

        // Non-waiting steps are nested under the preceding waiting step.
        private const float ChildIndent = 16f;
        private static readonly Color ChildGuide = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // Highlight for the step currently executing (play mode).
        private static readonly Color PlayingFill = new Color(0.30f, 0.70f, 0.35f, 0.22f);
        private static readonly Color PlayingBar  = new Color(0.35f, 0.85f, 0.40f, 0.95f);

        // Fire-and-forget steps run in a single frame; glow them for this long so they're
        // still visible (the active/blocking step stays solid via CurrentStepIndex).
        private const float GlowFade = 0.6f;

        // Repaint throughout play mode so glows animate and fade out cleanly after the run.
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        private void OnEnable()
        {
            _playOnStart = serializedObject.FindProperty("playOnStart");
            _loop        = serializedObject.FindProperty("loop");
            _steps       = serializedObject.FindProperty("steps");

            _list = new ReorderableList(serializedObject, _steps, true, true, true, true)
            {
                drawHeaderCallback   = rect => EditorGUI.LabelField(rect, "Audio Steps"),
                elementHeightCallback = GetElementHeight,
                drawElementCallback   = DrawElement,
                onAddDropdownCallback = OnAddDropdown
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_playOnStart);
            EditorGUILayout.PropertyField(_loop);
            EditorGUILayout.Space();
            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play"))  ((AudioCenterAudioSequencer)target).PlaySequence();
                    if (GUILayout.Button("Stop"))  ((AudioCenterAudioSequencer)target).StopSequence();
                }
            }
        }

        // ── Add menu ──────────────────────────────────────────────────────────

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<AudioCenterAudioStep>())
            {
                if (type.IsAbstract) continue;
                string path = GetStepMenuPath(type);
                Type captured = type;
                menu.AddItem(new GUIContent(path), false, () => AddStep(captured));
            }
            menu.ShowAsContext();
        }

        private void AddStep(Type type)
        {
            serializedObject.Update();
            int i = _steps.arraySize;
            _steps.InsertArrayElementAtIndex(i);
            SerializedProperty element = _steps.GetArrayElementAtIndex(i);
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = true; // show the new step's fields right away
            serializedObject.ApplyModifiedProperties();
        }

        private static string GetStepMenuPath(Type type)
        {
            try
            {
                if (Activator.CreateInstance(type) is AudioCenterAudioStep step && !string.IsNullOrEmpty(step.StepName))
                    return step.StepName;
            }
            catch { /* fall through to type name */ }
            return type.Name;
        }

        // ── Element drawing (generic field iteration with clip special-casing) ─

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            // Highlight executing steps (play mode): the active/blocking step stays solid,
            // while fire-and-forget steps that flash by in one frame glow briefly.
            if (Application.isPlaying && target is AudioCenterAudioSequencer seq)
            {
                float glow = 0f;
                if (seq.CurrentStepIndex == index)
                {
                    glow = 1f;
                }
                else
                {
                    float since = seq.TimeSinceStep(index);
                    if (since >= 0f && since < GlowFade)
                        glow = 1f - since / GlowFade;
                }

                if (glow > 0f)
                {
                    Rect hl = new Rect(rect.x - 2f, rect.y, rect.width + 6f, rect.height);
                    Color fill = PlayingFill; fill.a *= glow;
                    Color bar  = PlayingBar;  bar.a  *= glow;
                    EditorGUI.DrawRect(hl, fill);
                    EditorGUI.DrawRect(new Rect(hl.x, hl.y, 3f, hl.height), bar);
                }
            }

            // Inset so the drag handle (sort) and the step's foldout arrow sit in
            // separate columns instead of overlapping.
            rect.xMin += HandleColumnWidth;

            // Steps that don't wait are drawn as children of the preceding waiting step.
            if (IsChildStep(index))
            {
                EditorGUI.DrawRect(new Rect(rect.x + ChildIndent * 0.5f, rect.y, 1f, rect.height), ChildGuide);
                rect.xMin += ChildIndent;
            }

            SerializedProperty element = _steps.GetArrayElementAtIndex(index);
            AudioCenterStepGUI.Draw(rect, element);
        }

        private float GetElementHeight(int index)
        {
            SerializedProperty element = _steps.GetArrayElementAtIndex(index);
            return AudioCenterStepGUI.GetHeight(element);
        }

        // ── Step grouping (waiting step = parent, non-waiting = indented child) ─

        private AudioCenterAudioStep GetStep(int index)
            => _steps.GetArrayElementAtIndex(index).managedReferenceValue as AudioCenterAudioStep;

        // A step "waits" if it blocks the sequence: waitForCompletion on, or self-paced.
        private static bool StepWaits(AudioCenterAudioStep s)
            => s != null && (s.waitForCompletion || s.SelfPaced);

        // True when this step is non-waiting and a waiting step precedes it (its parent).
        private bool IsChildStep(int index)
        {
            AudioCenterAudioStep step = GetStep(index);
            if (step == null || StepWaits(step)) return false;

            for (int j = index - 1; j >= 0; j--)
            {
                AudioCenterAudioStep prev = GetStep(j);
                if (prev == null) continue;
                if (StepWaits(prev)) return true; // nearest preceding waiting step = parent
            }
            return false;
        }
    }

    /// <summary>
    /// Renders a single AudioCenterAudioStep managed-reference by iterating its serialized
    /// fields. Clip fields are shown conditionally: in File mode only the AudioClip
    /// is drawn; in Library mode a Group/Clip popup pair replaces it.
    /// </summary>
    internal static class AudioCenterStepGUI
    {
        private static float Line => EditorGUIUtility.singleLineHeight;
        private static float Pad  => EditorGUIUtility.standardVerticalSpacing;
        private static float Step => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        private static GUIStyle _foldout;
        private static GUIStyle Foldout =>
            _foldout ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        private enum FieldKind { Normal, LibraryBlock, Skip }

        private static FieldKind Classify(string name, bool isLibrary)
        {
            switch (name)
            {
                case "groupName": return isLibrary ? FieldKind.LibraryBlock : FieldKind.Skip;
                case "clipName":  return FieldKind.Skip;
                case "clip":      return isLibrary ? FieldKind.Skip : FieldKind.Normal;
                default:          return FieldKind.Normal;
            }
        }

        public static void Draw(Rect rect, SerializedProperty element)
        {
            float y = rect.y + Pad;

            // Foldout header — click to collapse/expand this step.
            Rect headerRect = new Rect(rect.x, y, rect.width, Line);
            element.isExpanded = EditorGUI.Foldout(headerRect, element.isExpanded, HeaderText(element), true, Foldout);
            y += Step;

            if (!element.isExpanded) return;

            EditorGUI.indentLevel++;
            bool isLibrary = false;
            SerializedProperty it = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enter = true;
            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = false;
                if (it.name == "clipReferenceType")
                    isLibrary = it.enumValueIndex == (int)AudioCenterClipReferenceType.Library;

                FieldKind kind = Classify(it.name, isLibrary);
                if (kind == FieldKind.Skip) continue;

                if (kind == FieldKind.LibraryBlock)
                {
                    SerializedProperty groupName = element.FindPropertyRelative("groupName");
                    SerializedProperty clipName  = element.FindPropertyRelative("clipName");
                    float h = AudioCenterClipLibrarySelector.GetHeight();
                    AudioCenterClipLibrarySelector.Draw(new Rect(rect.x, y, rect.width, h), groupName, clipName);
                    y += h;
                    continue;
                }

                float fh = EditorGUI.GetPropertyHeight(it, true);
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, fh), it, true);
                y += fh + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }

        public static float GetHeight(SerializedProperty element)
        {
            // Header + top padding; collapsed steps show only the header.
            float h = Pad + Step;
            if (!element.isExpanded) return h + Pad;

            bool isLibrary = false;
            SerializedProperty it = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enter = true;
            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = false;
                if (it.name == "clipReferenceType")
                    isLibrary = it.enumValueIndex == (int)AudioCenterClipReferenceType.Library;

                FieldKind kind = Classify(it.name, isLibrary);
                if (kind == FieldKind.Skip) continue;

                if (kind == FieldKind.LibraryBlock)
                {
                    h += AudioCenterClipLibrarySelector.GetHeight();
                    continue;
                }

                h += EditorGUI.GetPropertyHeight(it, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            return h;
        }

        private static string HeaderText(SerializedProperty element)
        {
            string typeName = element.managedReferenceFullTypename;
            int sep = typeName.LastIndexOf('.');
            if (sep >= 0) typeName = typeName.Substring(sep + 1);

            SerializedProperty label = element.FindPropertyRelative("label");
            string custom = label != null ? label.stringValue : null;
            return string.IsNullOrEmpty(custom) ? typeName : $"{typeName}  —  {custom}";
        }
    }
}
