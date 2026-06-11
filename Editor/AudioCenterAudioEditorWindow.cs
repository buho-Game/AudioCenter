using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AudioCenter.Editor
{
    public class AudioCenterAudioEditorWindow : EditorWindow
    {
        // ── State ──────────────────────────────────────────────────────────────

        private AudioCenterClipLibrary audioAsset;
        private SerializedObject librarySO;          // SerializedObject of the library asset itself
        private SerializedProperty groupsArray;      // library.groups[]

        private int groupIndex = -1;
        private ReorderableList clipList;
        private EditorGUISplitView splitView;
        private Vector2 clipScrollPosition;

        private Color buttonFocusColor = new Color(0.6f, 0.9f, 1f, 1f);
        private Color cachedBgColor;

        // ── Helpers ────────────────────────────────────────────────────────────

        private bool HasGroups    => audioAsset != null && audioAsset.GroupCount > 0;
        private bool GroupSelected => groupIndex >= 0 && audioAsset != null && groupIndex < audioAsset.GroupCount;

        /// Returns the serialized AudioCenterClipGroup at the current groupIndex
        private SerializedProperty CurrentGroupProp =>
            groupsArray?.GetArrayElementAtIndex(groupIndex);

        private SerializedProperty GroupNameProp =>
            CurrentGroupProp?.FindPropertyRelative("groupName");

        private SerializedProperty ClipAssetsProp =>
            CurrentGroupProp?.FindPropertyRelative("assets");

        private SerializedProperty ClipNameProp(int i) =>
            ClipAssetsProp.GetArrayElementAtIndex(i).FindPropertyRelative("clipName");

        private SerializedProperty ClipProp(int i) =>
            ClipAssetsProp.GetArrayElementAtIndex(i).FindPropertyRelative("clip");

        // ── Menu ───────────────────────────────────────────────────────────────

        [MenuItem("AudioCenter/Audio Library")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioCenterAudioEditorWindow>(false, "AudioCenter Audio Library", true);
            window.Initialize();
        }

        // ── Init ───────────────────────────────────────────────────────────────

        private void Initialize()
        {
            minSize  = new Vector2(520, 360);
            splitView = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, 200f);
            LoadAsset();
        }

        private void LoadAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioCenterClipLibrary");
            if (guids.Length > 0)
                SetLibrary(AssetDatabase.LoadAssetAtPath<AudioCenterClipLibrary>(
                    AssetDatabase.GUIDToAssetPath(guids[0])));
        }

        private void SetLibrary(AudioCenterClipLibrary lib)
        {
            audioAsset = lib;
            librarySO  = lib != null ? new SerializedObject(lib) : null;
            groupsArray = librarySO?.FindProperty("groups");
            groupIndex  = HasGroups ? 0 : -1;
            if (GroupSelected) BuildClipList();
        }

        // ── GUI ────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (splitView == null) Initialize();

            DrawToolbar();

            if (audioAsset == null)
            {
                EditorGUILayout.HelpBox("No AudioCenterClipLibrary selected. Create one via  Create → AudioCenter → Audio → Clip Library.", MessageType.Info);
                return;
            }

            librarySO.Update();

            splitView.BeginSplitView();
            DrawGroupPanel();
            splitView.Split();
            if (GroupSelected) DrawEditPanel();
            splitView.EndSplitView();

            librarySO.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Library:", GUILayout.Width(52));
            var picked = (AudioCenterClipLibrary)EditorGUILayout.ObjectField(
                audioAsset, typeof(AudioCenterClipLibrary), false, GUILayout.Width(230));
            if (picked != audioAsset) SetLibrary(picked);

            GUILayout.FlexibleSpace();

            if (audioAsset != null && GUILayout.Button("+ Group", EditorStyles.toolbarButton, GUILayout.Width(70)))
                AddGroup();

            EditorGUILayout.EndHorizontal();
        }

        // ── Left panel : group list ────────────────────────────────────────────

        private GUIStyle _groupBtn;
        private void DrawGroupPanel()
        {
            _groupBtn ??= new GUIStyle(EditorStyles.miniButton) { fontSize = 13, fixedHeight = 28 };

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Groups", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();

            cachedBgColor = GUI.backgroundColor;
            for (int i = 0; i < audioAsset.GroupCount; i++)
            {
                if (groupIndex == i) GUI.backgroundColor = buttonFocusColor;
                if (GUILayout.Button(audioAsset[i].GroupName, _groupBtn))
                    SelectGroup(i);
                GUI.backgroundColor = cachedBgColor;
                GUILayout.Space(4);
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        private void SelectGroup(int index)
        {
            groupIndex = index;
            BuildClipList();
        }

        // ── Right panel : clip editor ──────────────────────────────────────────

        private void DrawEditPanel()
        {
            GUILayout.Space(12);
            GUILayout.BeginVertical();

            // Group name field
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group Name",
                new GUIStyle(EditorStyles.label) { fontSize = 15 }, GUILayout.MaxWidth(110));
            EditorGUILayout.PropertyField(GroupNameProp, GUIContent.none);
            GUILayout.EndHorizontal();

            // Delete group button (right-aligned)
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("Delete Group", GUILayout.Width(100)))
            {
                RemoveGroup(groupIndex);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = cachedBgColor;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Clips",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 }, GUILayout.MaxWidth(60));
            GUILayout.Space(4);

            clipScrollPosition = GUILayout.BeginScrollView(clipScrollPosition);
            clipList?.DoLayoutList();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        // ── Clip list ──────────────────────────────────────────────────────────

        private void BuildClipList()
        {
            // ReorderableList must reference the SerializedProperty for assets inside
            // the currently selected group element
            SerializedProperty clipsProp = ClipAssetsProp;
            if (clipsProp == null) return;

            clipList = new ReorderableList(librarySO, clipsProp, true, false, true, true)
            {
                elementHeight          = 20,
                multiSelect            = true,
                drawElementCallback    = DrawClipElement,
                onAddCallback          = _ => AddClip(),
                onRemoveCallback       = RemoveSelectedClips
            };
        }

        private const int NameWidth = 110;

        private void DrawClipElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            float w  = rect.width;
            rect.x  += 2; rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            rect.width = NameWidth;
            EditorGUI.PropertyField(rect, ClipNameProp(index), GUIContent.none);

            rect.x    += NameWidth + 4;
            rect.width = w - NameWidth - 6;
            EditorGUI.PropertyField(rect, ClipProp(index), GUIContent.none);
        }

        private void AddClip()
        {
            librarySO.Update();
            SerializedProperty assets = ClipAssetsProp;
            assets.arraySize++;
            librarySO.ApplyModifiedProperties();

            ClipNameProp(assets.arraySize - 1).stringValue      = "NewClip";
            ClipProp(assets.arraySize - 1).objectReferenceValue = null;
            librarySO.ApplyModifiedProperties();
        }

        private void RemoveSelectedClips(ReorderableList list)
        {
            var indices = new List<int>(list.selectedIndices);
            if (indices.Count == 0 && list.index >= 0)
                indices.Add(list.index);
            if (indices.Count == 0) return;

            librarySO.Update();
            indices.Sort();
            // Delete from the end so earlier indices stay valid
            for (int i = indices.Count - 1; i >= 0; i--)
                ClipAssetsProp.DeleteArrayElementAtIndex(indices[i]);
            librarySO.ApplyModifiedProperties();
            BuildClipList();
        }

        // ── Group add / remove ────────────────────────────────────────────────

        private void AddGroup()
        {
            librarySO.Update();
            groupsArray.arraySize++;
            int newIndex = groupsArray.arraySize - 1;
            groupsArray.GetArrayElementAtIndex(newIndex)
                       .FindPropertyRelative("groupName").stringValue = "NewGroup";
            groupsArray.GetArrayElementAtIndex(newIndex)
                       .FindPropertyRelative("assets").arraySize = 0;
            librarySO.ApplyModifiedProperties();

            SelectGroup(newIndex);
        }

        private void RemoveGroup(int index)
        {
            librarySO.Update();
            groupsArray.DeleteArrayElementAtIndex(index);
            librarySO.ApplyModifiedProperties();

            groupIndex = Mathf.Clamp(index - 1, HasGroups ? 0 : -1, audioAsset.GroupCount - 1);
            if (GroupSelected) BuildClipList();
            else clipList = null;
        }
    }
}
