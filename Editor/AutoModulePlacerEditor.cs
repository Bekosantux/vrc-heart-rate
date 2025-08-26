#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static BekoShop.VRCHeartRate.LocalizationManager;

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(AutoModulePlacer))]
    public class AutoAssetPlacerEditor : Editor
    {
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            var placer = (AutoModulePlacer)target;

            // 配置検証
            bool isValid = placer.IsValidPlacement();

            if (!isValid)
            {
                // GetStatusMessageはコンポーネント側で生成されるため、そのまま表示
                EditorGUILayout.HelpBox(S("placer.warning.put_inside"), MessageType.Warning);
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox(S("placer.message.dont_delete"), MessageType.Info);
            }

            var parentPrefab = placer.GetParentContainerPrefab();
            if (parentPrefab == null)
            {
                EditorGUILayout.HelpBox(S("placer.error.no_parent_prefab"), MessageType.Error);
            }

            var optsEnabled = placer.GetOptionEnabled();
            var optsPrefabs = placer.GetOptionPrefabs();

            bool anyEnabled = false;
            bool hasEnabledButMissingPrefab = false;

            for (int i = 0; i < 8; i++)
            {
                if (optsEnabled != null && i < optsEnabled.Length && optsEnabled[i])
                {
                    anyEnabled = true;
                    if (optsPrefabs == null || i >= optsPrefabs.Length || optsPrefabs[i] == null)
                    {
                        hasEnabledButMissingPrefab = true;
                    }
                }
            }

            if (!anyEnabled)
            {
                EditorGUILayout.HelpBox(S("placer.warning.no_option_selected"), MessageType.Warning);
            }
            if (hasEnabledButMissingPrefab)
            {
                EditorGUILayout.HelpBox(S("placer.error.no_child_prefab"), MessageType.Error);
            }

            EditorGUILayout.Space();

            // 開発者設定
            showSettings = EditorGUILayout.Foldout(showSettings, S("placer.foldout.developer"), true);
            if (showSettings)
            {
                EditorGUI.indentLevel++;

                // Debug モード判定
                var inspectorWindow = EditorWindow.focusedWindow;
                bool showPrefabFields = false;

                if (inspectorWindow != null && inspectorWindow.GetType().Name == "InspectorWindow")
                {
                    var inspectorModeInfo = inspectorWindow.GetType().GetProperty("inspectorMode",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (inspectorModeInfo != null)
                    {
                        var mode = inspectorModeInfo.GetValue(inspectorWindow);
                        showPrefabFields = mode != null && mode.ToString() == "Debug";
                    }
                }

                if (showPrefabFields)
                {
                    EditorGUI.BeginChangeCheck();

                    // 親プレハブ
                    var newParent = (GameObject)EditorGUILayout.ObjectField(S("placer.tooltip.parent_container"), parentPrefab, typeof(GameObject), false);
                    if (newParent != parentPrefab)
                    {
                        placer.SetParentContainerPrefab(newParent);
                    }

                    EditorGUILayout.Space(4);

                    var enabled = placer.GetOptionEnabled();
                    var prefabs = placer.GetOptionPrefabs();

                    for (int i = 0; i < 8; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        bool newFlag = EditorGUILayout.ToggleLeft(AutoModulePlacer.OptionLabels[i], enabled[i], GUILayout.Width(120));
                        if (newFlag != enabled[i])
                        {
                            placer.SetOptionEnabled((AutoModulePlacer.OptionSlot)i, newFlag);
                        }

                        var newChild = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
                        if (newChild != prefabs[i])
                        {
                            placer.SetOptionPrefab((AutoModulePlacer.OptionSlot)i, newChild);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(target);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(S("placer.message.debug_mode_only"), MessageType.Info);

                    // オプションの有効/無効のみ表示（プレハブ参照は非表示）
                    EditorGUI.BeginChangeCheck();
                    var enabled = placer.GetOptionEnabled();

                    for (int i = 0; i < 8; i++)
                    {
                        bool newFlag = EditorGUILayout.ToggleLeft(AutoModulePlacer.OptionLabels[i], enabled[i]);
                        if (newFlag != enabled[i])
                        {
                            placer.SetOptionEnabled((AutoModulePlacer.OptionSlot)i, newFlag);
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(target);
                    }
                }

                EditorGUILayout.Space();

                // 強制実行
                EditorGUI.BeginDisabledGroup(!isValid);
                if (GUILayout.Button(S("placer.button.force_check")))
                {
                    placer.ValidateAndProcess();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            ShowLanguageUI();
        }
    }
}
#endif