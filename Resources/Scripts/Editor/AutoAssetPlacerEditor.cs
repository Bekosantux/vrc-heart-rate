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

        private void OnEnable()
        {
            AutoAssetPlacer placer = (AutoAssetPlacer)target;

            // ReorderableListを初期化
            reorderableList = new ReorderableList(
                placer.targetPrefabs,
                typeof(GameObject),
                true,  // draggable
                true,  // displayHeader
                true,  // displayAddButton
                true   // displayRemoveButton
            );

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Target Prefabs");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= 0 && index < placer.targetPrefabs.Count)
                {
                    var element = placer.targetPrefabs[index];
                    var newElement = EditorGUI.ObjectField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element,
                        typeof(GameObject),
                        false
                    ) as GameObject;

                    // 重複チェック
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
                }
            };

            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                placer.targetPrefabs.Add(null);
                EditorUtility.SetDirty(target);
            };

            reorderableList.onRemoveCallback = (ReorderableList list) =>
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

            // AvatarRoot配置検証
            if (!placer.IsValidPlacement())
            {
                EditorGUILayout.HelpBox(placer.GetErrorMessage(), MessageType.Warning);
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("このスクリプトは削除しないでください。\nPlease don't delete this script.", MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();

            // 設定をフォールドアウトで隠す
            showSettings = EditorGUILayout.Foldout(showSettings, "Developer Settings", true);

            if (showSettings)
            {
                EditorGUI.indentLevel++;

                // ReorderableListを描画
                if (reorderableList != null)
                {
                    reorderableList.DoLayoutList();
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                // 有効な配置の場合のみボタンを有効化
                EditorGUI.BeginDisabledGroup(!placer.IsValidPlacement());
                if (GUILayout.Button("Force Check and Place"))
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