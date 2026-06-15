using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AudioCenter.UI;

namespace AudioCenter.Editor.UI
{
    /// <summary>
    /// Editor tool for bulk-adding <see cref="AudioCenterCustomButton"/> components to UGUI
    /// Buttons. Scans either a target GameObject's hierarchy, the active scene, or all loaded
    /// scenes, and adds (or removes) the component on every Button it finds — assigning a
    /// default animation config and SFX coordinates in one pass.
    /// </summary>
    public class AudioCenterCustomButtonTool : EditorWindow
    {
        private enum ScanMode
        {
            TargetGameObject,
            ActiveScene,
            AllLoadedScenes
        }

        // ── Options ──────────────────────────────────────────────────────────────
        private ScanMode scanMode = ScanMode.TargetGameObject;
        private GameObject targetGameObject;

        private bool includeInactive = true;
        private bool replaceExisting = false;

        private AudioCenterButtonAnimationConfig defaultConfig;
        private string sfxGroupName = "UI";
        private string sfxClipName = "ButtonClick";
        private bool enableHoverAnimation = true;
        private bool enableClickAnimation = true;
        private bool enableSFX = true;

        // ── Results / log ──────────────────────────────────────────────────────────
        private int processedCount;
        private int skippedCount;
        private int errorCount;

        private Vector2 windowScroll;   // whole-window scroll
        private Vector2 scrollPosition; // log scroll
        private string logMessage = "";

        // ── EditorPrefs keys ─────────────────────────────────────────────────────
        private const string PrefScanMode = "ACCustomButtonTool_ScanMode";
        private const string PrefIncludeInactive = "ACCustomButtonTool_IncludeInactive";
        private const string PrefReplaceExisting = "ACCustomButtonTool_ReplaceExisting";
        private const string PrefConfigGuid = "ACCustomButtonTool_ConfigGuid";
        private const string PrefSfxGroup = "ACCustomButtonTool_SfxGroup";
        private const string PrefSfxClip = "ACCustomButtonTool_SfxClip";
        private const string PrefHover = "ACCustomButtonTool_EnableHover";
        private const string PrefClick = "ACCustomButtonTool_EnableClick";
        private const string PrefSfx = "ACCustomButtonTool_EnableSfx";

        [MenuItem("AudioCenter/Custom Button Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioCenterCustomButtonTool>("Custom Button Tool");
            window.minSize = new Vector2(400, 520);
            window.Show();
        }

        private void OnGUI()
        {
            windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
            EditorGUILayout.Space(8);

            DrawHeader();
            DrawScopeSection();
            DrawOptionsSection();
            DrawDefaultsSection();
            DrawActionsSection();
            DrawResultsSection();
            DrawLogSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        // ── Section drawing ──────────────────────────────────────────────────────

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Custom Button Tool", TitleStyle);
                EditorGUILayout.LabelField(
                    "Bulk-add AudioCenterCustomButton to UGUI Buttons in the chosen scope.",
                    SubtitleStyle);
            }
            EditorGUILayout.Space(6);
        }

        private void DrawScopeSection()
        {
            BeginSection("Scan Scope");

            scanMode = (ScanMode)EditorGUILayout.EnumPopup(ScanModeLabel, scanMode);

            EditorGUILayout.Space(2);
            if (scanMode == ScanMode.TargetGameObject)
            {
                targetGameObject = (GameObject)EditorGUILayout.ObjectField(
                    TargetLabel, targetGameObject, typeof(GameObject), true);

                if (targetGameObject == null)
                    EditorGUILayout.HelpBox("Assign a root GameObject to scan its hierarchy.", MessageType.Warning);
            }
            else
            {
                string desc = scanMode == ScanMode.ActiveScene
                    ? "Scans every root object in the active scene."
                    : "Scans every root object across all loaded scenes.";
                EditorGUILayout.HelpBox(desc, MessageType.Info);
            }

            EndSection();
        }

        private void DrawOptionsSection()
        {
            BeginSection("Scan Options");
            includeInactive = EditorGUILayout.Toggle(IncludeInactiveLabel, includeInactive);
            replaceExisting = EditorGUILayout.Toggle(ReplaceExistingLabel, replaceExisting);
            EndSection();
        }

        private void DrawDefaultsSection()
        {
            BeginSection("Component Defaults");
            EditorGUILayout.LabelField("Applied to every component this tool adds.", SubtitleStyle);
            EditorGUILayout.Space(2);

            defaultConfig = (AudioCenterButtonAnimationConfig)EditorGUILayout.ObjectField(
                ConfigLabel, defaultConfig, typeof(AudioCenterButtonAnimationConfig), false);
            if (defaultConfig == null)
                EditorGUILayout.HelpBox("No config assigned — buttons use built-in default animation values.", MessageType.None);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("UI SFX", EditorStyles.miniBoldLabel);
            sfxGroupName = EditorGUILayout.TextField(SfxGroupLabel, sfxGroupName);
            sfxClipName = EditorGUILayout.TextField(SfxClipLabel, sfxClipName);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Enabled Features", EditorStyles.miniBoldLabel);
            enableHoverAnimation = EditorGUILayout.Toggle(HoverLabel, enableHoverAnimation);
            enableClickAnimation = EditorGUILayout.Toggle(ClickLabel, enableClickAnimation);
            enableSFX = EditorGUILayout.Toggle(SfxLabel, enableSFX);
            EndSection();
        }

        private void DrawActionsSection()
        {
            BeginSection("Actions");

            bool blocked = scanMode == ScanMode.TargetGameObject && targetGameObject == null;
            using (new EditorGUI.DisabledScope(blocked))
            {
                Color prevBg = GUI.backgroundColor;

                GUI.backgroundColor = AddColor;
                if (GUILayout.Button(AddButtonLabel, GUILayout.Height(32)))
                    ProcessButtons();

                GUI.backgroundColor = RemoveColor;
                if (GUILayout.Button(RemoveButtonLabel, GUILayout.Height(26)))
                    RemoveCustomButtons();

                GUI.backgroundColor = prevBg;
            }

            EndSection();
        }

        private void DrawResultsSection()
        {
            BeginSection("Results");
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawResultBadge("Processed", processedCount);
                DrawResultBadge("Skipped", skippedCount);
                DrawResultBadge("Errors", errorCount);
            }
            EndSection();
        }

        private void DrawResultBadge(string label, int value)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var valueStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16
                };
                EditorGUILayout.LabelField(value.ToString(), valueStyle);
                EditorGUILayout.LabelField(label, CenteredMiniStyle);
            }
        }

        private void DrawLogSection()
        {
            if (string.IsNullOrEmpty(logMessage))
                return;

            BeginSection("Log");
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(logMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log", GUILayout.Height(20)))
                logMessage = "";
            EndSection();
        }

        // ── Section / style helpers ──────────────────────────────────────────────

        private static void BeginSection(string title)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        private static void EndSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private static GUIStyle _titleStyle;
        private static GUIStyle TitleStyle => _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14
        };

        private static GUIStyle _subtitleStyle;
        private static GUIStyle SubtitleStyle => _subtitleStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap = true
        };

        private static GUIStyle _centeredMiniStyle;
        private static GUIStyle CenteredMiniStyle => _centeredMiniStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        // Tinted action buttons (slightly more saturated in dark skin for contrast).
        private static Color AddColor => new Color(0.55f, 0.85f, 0.55f);
        private static Color RemoveColor => new Color(0.9f, 0.55f, 0.55f);

        // ── Field labels + tooltips ──────────────────────────────────────────────

        private static readonly GUIContent ScanModeLabel = new GUIContent(
            "Scan Mode",
            "Where to look for Buttons: a single GameObject's hierarchy, the active scene, or every loaded scene.");
        private static readonly GUIContent TargetLabel = new GUIContent(
            "Root GameObject",
            "The hierarchy root to scan. All Button components beneath it are processed.");
        private static readonly GUIContent IncludeInactiveLabel = new GUIContent(
            "Include Inactive",
            "Also process Buttons on inactive GameObjects (disabled in the hierarchy).");
        private static readonly GUIContent ReplaceExistingLabel = new GUIContent(
            "Replace Existing",
            "If a Button already has an AudioCenterCustomButton, remove and re-add it with the current defaults. When off, such Buttons are skipped.");
        private static readonly GUIContent ConfigLabel = new GUIContent(
            "Animation Config",
            "ScriptableObject animation preset assigned to each added component. Leave empty to use built-in defaults.");
        private static readonly GUIContent SfxGroupLabel = new GUIContent(
            "SFX Group",
            "Clip group in the AudioCenter library played on click (via the UI track).");
        private static readonly GUIContent SfxClipLabel = new GUIContent(
            "SFX Clip",
            "Clip name within the group played on click.");
        private static readonly GUIContent HoverLabel = new GUIContent(
            "Hover Animation",
            "Enable the hover scale pop on the added components.");
        private static readonly GUIContent ClickLabel = new GUIContent(
            "Click Animation",
            "Enable the press-down/bounce scale animation on click.");
        private static readonly GUIContent SfxLabel = new GUIContent(
            "Play SFX",
            "Play the UI sound on click.");
        private static readonly GUIContent AddButtonLabel = new GUIContent(
            "Add Custom Buttons",
            "Add AudioCenterCustomButton to every Button found in the scope. Undoable.");
        private static readonly GUIContent RemoveButtonLabel = new GUIContent(
            "Remove Custom Buttons",
            "Remove every AudioCenterCustomButton found in the scope. Undoable.");

        // ── Scope collection ───────────────────────────────────────────────────────

        // Gathers every Button in the current scope, paired with the scene it belongs to
        // so each affected scene can be marked dirty individually.
        private List<Button> CollectButtons()
        {
            var buttons = new List<Button>();

            switch (scanMode)
            {
                case ScanMode.TargetGameObject:
                    if (targetGameObject != null)
                        buttons.AddRange(targetGameObject.GetComponentsInChildren<Button>(includeInactive));
                    break;

                case ScanMode.ActiveScene:
                    CollectFromScene(EditorSceneManager.GetActiveScene(), buttons);
                    break;

                case ScanMode.AllLoadedScenes:
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        CollectFromScene(SceneManager.GetSceneAt(i), buttons);
                    break;
            }

            return buttons;
        }

        private void CollectFromScene(Scene scene, List<Button> into)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            foreach (GameObject root in scene.GetRootGameObjects())
                into.AddRange(root.GetComponentsInChildren<Button>(includeInactive));
        }

        // ── Add ────────────────────────────────────────────────────────────────────

        private void ProcessButtons()
        {
            ResetCounters();

            List<Button> buttons = CollectButtons();
            LogMessage($"[{scanMode}] Found {buttons.Count} Button components");

            var dirtyScenes = new HashSet<Scene>();

            foreach (Button button in buttons)
            {
                if (ProcessButton(button) && button != null)
                    dirtyScenes.Add(button.gameObject.scene);
            }

            LogMessage($"Done. Processed: {processedCount}, Skipped: {skippedCount}, Errors: {errorCount}");

            MarkScenesDirty(dirtyScenes);
        }

        // Returns true if the GameObject was modified (component added).
        private bool ProcessButton(Button button)
        {
            if (button == null)
            {
                errorCount++;
                LogMessage("Null button encountered", MessageType.Error);
                return false;
            }

            AudioCenterCustomButton existing = button.GetComponent<AudioCenterCustomButton>();
            if (existing != null)
            {
                if (replaceExisting)
                {
                    LogMessage($"Replacing existing on: {button.gameObject.name}");
                    Undo.DestroyObjectImmediate(existing);
                }
                else
                {
                    skippedCount++;
                    LogMessage($"Skipping {button.gameObject.name} - already has component", MessageType.Warning);
                    return false;
                }
            }

            try
            {
                var customButton = Undo.AddComponent<AudioCenterCustomButton>(button.gameObject);
                ConfigureCustomButton(customButton);
                processedCount++;
                LogMessage($"Added to: {button.gameObject.name}");
                return true;
            }
            catch (System.Exception e)
            {
                errorCount++;
                LogMessage($"Error on {button.gameObject.name}: {e.Message}", MessageType.Error);
                return false;
            }
        }

        private void ConfigureCustomButton(AudioCenterCustomButton customButton)
        {
            var so = new SerializedObject(customButton);
            so.FindProperty("animationConfig").objectReferenceValue = defaultConfig;
            so.FindProperty("sfxGroupName").stringValue = sfxGroupName;
            so.FindProperty("sfxClipName").stringValue = sfxClipName;
            so.FindProperty("enableHoverAnimation").boolValue = enableHoverAnimation;
            so.FindProperty("enableClickAnimation").boolValue = enableClickAnimation;
            so.FindProperty("enableSFX").boolValue = enableSFX;
            so.ApplyModifiedProperties();
        }

        // ── Remove ───────────────────────────────────────────────────────────────────

        private void RemoveCustomButtons()
        {
            ResetCounters();

            var dirtyScenes = new HashSet<Scene>();

            // Collect target components from the same scope.
            var toRemove = new List<AudioCenterCustomButton>();
            switch (scanMode)
            {
                case ScanMode.TargetGameObject:
                    if (targetGameObject != null)
                        toRemove.AddRange(targetGameObject.GetComponentsInChildren<AudioCenterCustomButton>(includeInactive));
                    break;
                case ScanMode.ActiveScene:
                    CollectComponentsFromScene(EditorSceneManager.GetActiveScene(), toRemove);
                    break;
                case ScanMode.AllLoadedScenes:
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        CollectComponentsFromScene(SceneManager.GetSceneAt(i), toRemove);
                    break;
            }

            LogMessage($"[{scanMode}] Found {toRemove.Count} AudioCenterCustomButton components");

            foreach (AudioCenterCustomButton cb in toRemove)
            {
                if (cb == null) continue;
                dirtyScenes.Add(cb.gameObject.scene);
                LogMessage($"Removing from: {cb.gameObject.name}");
                Undo.DestroyObjectImmediate(cb);
                processedCount++;
            }

            LogMessage($"Removal complete. Removed: {processedCount}");
            MarkScenesDirty(dirtyScenes);
        }

        private void CollectComponentsFromScene(Scene scene, List<AudioCenterCustomButton> into)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            foreach (GameObject root in scene.GetRootGameObjects())
                into.AddRange(root.GetComponentsInChildren<AudioCenterCustomButton>(includeInactive));
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private void MarkScenesDirty(HashSet<Scene> scenes)
        {
            foreach (Scene scene in scenes)
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
        }

        private void ResetCounters()
        {
            processedCount = 0;
            skippedCount = 0;
            errorCount = 0;
        }

        private void LogMessage(string message, MessageType messageType = MessageType.None)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string prefix = messageType switch
            {
                MessageType.Error => "[ERROR]",
                MessageType.Warning => "[WARN]",
                MessageType.Info => "[INFO]",
                _ => "[LOG]"
            };

            logMessage += $"[{timestamp}] {prefix} {message}\n";
            scrollPosition.y = float.MaxValue;
        }

        // ── Settings persistence ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            scanMode = (ScanMode)EditorPrefs.GetInt(PrefScanMode, (int)ScanMode.TargetGameObject);
            includeInactive = EditorPrefs.GetBool(PrefIncludeInactive, true);
            replaceExisting = EditorPrefs.GetBool(PrefReplaceExisting, false);
            sfxGroupName = EditorPrefs.GetString(PrefSfxGroup, "UI");
            sfxClipName = EditorPrefs.GetString(PrefSfxClip, "ButtonClick");
            enableHoverAnimation = EditorPrefs.GetBool(PrefHover, true);
            enableClickAnimation = EditorPrefs.GetBool(PrefClick, true);
            enableSFX = EditorPrefs.GetBool(PrefSfx, true);

            string guid = EditorPrefs.GetString(PrefConfigGuid, "");
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    defaultConfig = AssetDatabase.LoadAssetAtPath<AudioCenterButtonAnimationConfig>(path);
            }
        }

        private void OnDisable()
        {
            // GameObject scene references can't be persisted to EditorPrefs.
            EditorPrefs.SetInt(PrefScanMode, (int)scanMode);
            EditorPrefs.SetBool(PrefIncludeInactive, includeInactive);
            EditorPrefs.SetBool(PrefReplaceExisting, replaceExisting);
            EditorPrefs.SetString(PrefSfxGroup, sfxGroupName);
            EditorPrefs.SetString(PrefSfxClip, sfxClipName);
            EditorPrefs.SetBool(PrefHover, enableHoverAnimation);
            EditorPrefs.SetBool(PrefClick, enableClickAnimation);
            EditorPrefs.SetBool(PrefSfx, enableSFX);

            string guid = defaultConfig != null
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(defaultConfig))
                : "";
            EditorPrefs.SetString(PrefConfigGuid, guid);
        }
    }
}
