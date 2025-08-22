#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(AutoAssetPlacer))]
    public class AutoAssetPlacerEditor : Editor
    {
        private bool showSettings = false;

        private static readonly GUIContent gcDeveloperSettings = new GUIContent("Developer Settings");
        private static readonly GUIContent gcParentPrefab = new GUIContent("Parent Container Prefab");
        private static readonly GUIContent gcForceButton = new GUIContent("Force Check and Place");

        public override void OnInspectorGUI()
        {
            var placer = (AutoAssetPlacer)target;

            // 配置検証
            bool isValid = placer.IsValidPlacement();

            if (!isValid)
            {
                EditorGUILayout.HelpBox(placer.GetStatusMessage(), MessageType.Warning);
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("このスクリプトは削除しないでください。\nPlease don't delete this script.", MessageType.Info);
            }

            var parentPrefab = placer.GetParentContainerPrefab();
            if (parentPrefab == null)
            {
                EditorGUILayout.HelpBox("親プレハブが設定されていません。", MessageType.Error);
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
                EditorGUILayout.HelpBox("オプションが一つも選択されていません。少なくとも1つ選択してください。", MessageType.Warning);
            }
            if (hasEnabledButMissingPrefab)
            {
                EditorGUILayout.HelpBox("有効化されたオプションに対応する子プレハブが未設定です。", MessageType.Error);
            }

            EditorGUILayout.Space();

            // 開発者設定
            showSettings = EditorGUILayout.Foldout(showSettings, gcDeveloperSettings, true);
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
                    var newParent = (GameObject)EditorGUILayout.ObjectField(gcParentPrefab, parentPrefab, typeof(GameObject), false);
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
                        bool newFlag = EditorGUILayout.ToggleLeft(AutoAssetPlacer.OptionLabels[i], enabled[i], GUILayout.Width(120));
                        if (newFlag != enabled[i])
                        {
                            placer.SetOptionEnabled((AutoAssetPlacer.OptionSlot)i, newFlag);
                        }

                        var newChild = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
                        if (newChild != prefabs[i])
                        {
                            placer.SetOptionPrefab((AutoAssetPlacer.OptionSlot)i, newChild);
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
                    EditorGUILayout.HelpBox("プレハブ設定フィールドはDebugモードでのみ表示されます。", MessageType.Info);

                    // オプションの有効/無効のみ表示（プレハブ参照は非表示）
                    EditorGUI.BeginChangeCheck();
                    var enabled = placer.GetOptionEnabled();

                    for (int i = 0; i < 8; i++)
                    {
                        bool newFlag = EditorGUILayout.ToggleLeft(AutoAssetPlacer.OptionLabels[i], enabled[i]);
                        if (newFlag != enabled[i])
                        {
                            placer.SetOptionEnabled((AutoAssetPlacer.OptionSlot)i, newFlag);
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
                if (GUILayout.Button(gcForceButton))
                {
                    placer.ValidateAndProcess();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif