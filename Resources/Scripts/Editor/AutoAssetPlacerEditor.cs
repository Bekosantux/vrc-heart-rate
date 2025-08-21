#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(AutoAssetPlacer))]
    public class AutoAssetPlacerEditor : Editor
    {
        private bool showSettings = false;
        private ReorderableList reorderableList;

        // GUIContent をキャッシュして GC Alloc を減らす
        private static readonly GUIContent settingsLabel = new GUIContent("Developer Settings");
        private static readonly GUIContent forceButtonLabel = new GUIContent("Force Check and Place");

        private void OnEnable()
        {
            AutoAssetPlacer placer = (AutoAssetPlacer)target;

            // null チェック（エラー防止）
            if (placer.targetPrefabs == null) return;

            // ReorderableListを初期化
            reorderableList = new ReorderableList(
                placer.targetPrefabs,
                typeof(GameObject),
                true, true, true, true
            );

            reorderableList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Target Prefabs");

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index < 0 || index >= placer.targetPrefabs.Count) return;

                var element = placer.targetPrefabs[index];
                var newElement = EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element,
                    typeof(GameObject),
                    false
                ) as GameObject;

                if (newElement != element)
                {
                    if (newElement == null || !placer.targetPrefabs.Contains(newElement))
                    {
                        placer.targetPrefabs[index] = newElement;
                        EditorUtility.SetDirty(target);
                    }
                    else if (newElement != null)
                    {
                        Debug.LogWarning("このプレハブは既にリストに存在します。");
                    }
                }
            };

            reorderableList.onAddCallback = list =>
            {
                placer.targetPrefabs.Add(null);
                EditorUtility.SetDirty(target);
            };

            reorderableList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < placer.targetPrefabs.Count)
                {
                    placer.targetPrefabs.RemoveAt(list.index);
                    EditorUtility.SetDirty(target);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            AutoAssetPlacer placer = (AutoAssetPlacer)target;

            // 配置検証結果表示
            bool isValid = placer.IsValidPlacement();
            if (!isValid)
            {
                EditorGUILayout.HelpBox(placer.GetErrorMessage(), MessageType.Warning);
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox(placer.GetErrorMessage(), MessageType.Info);
                EditorGUILayout.Space();
            }

            EditorGUI.BeginChangeCheck();

            // 設定フォールドアウト
            showSettings = EditorGUILayout.Foldout(showSettings, settingsLabel, true);

            if (showSettings)
            {
                EditorGUI.indentLevel++;

                // リスト描画
                if (reorderableList != null)
                {
                    reorderableList.DoLayoutList();
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                // ボタン
                EditorGUI.BeginDisabledGroup(!isValid);
                if (GUILayout.Button(forceButtonLabel))
                {
                    placer.ValidateAndProcess();
                }
                EditorGUI.EndDisabledGroup();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif