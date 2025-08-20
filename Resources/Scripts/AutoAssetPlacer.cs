using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using nadena.dev.ndmf.util;
#endif

namespace BekoShop.VRCHeartRate
{
    public class AutoAssetPlacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private List<GameObject> _targetPrefabs = new List<GameObject>();  // シリアライズ用のバッキングフィールド

        // 外部からアクセス用のプロパティ
        public List<GameObject> targetPrefabs
        {
            get => _targetPrefabs;
            private set => _targetPrefabs = value;
        }

        private bool isValidPlacement = false;
        private string errorMessage = "";

        // Undo/Redoで自動配置しないように状態を保持
        private static bool suppressAutoPlacement = false;

        private void OnEnable()
        {
            // Undo/Redoのイベントで自動配置を抑制
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
            // ヒエラルキーの移動でも再検証
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
            // 初回も検証
            ValidateAndProcess();
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnUndoRedo()
        {
            // Undo/Redo時は自動配置しない
            suppressAutoPlacement = true;
            ValidateAndProcess();
            suppressAutoPlacement = false;
        }

        private void OnHierarchyChanged()
        {
            // ヒエラルキー移動でも再検証・警告更新する
            ValidateAvatarRootPlacement();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void Start()
        {
            // プレイ時は何もしない
            if (Application.isPlaying)
                return;
            ValidateAndProcess();
        }

        private void OnValidate()
        {
            // プレイ時は何もしない
            if (Application.isPlaying)
                return;

            // 重複を除去
            RemoveDuplicatesFromList();
            ValidateAndProcess();
        }

        public void ValidateAndProcess()
        {
            if (!ValidateAvatarRootPlacement())
            {
                // アバター外なら何もしない
                return;
            }
            if (!suppressAutoPlacement)
            {
                CheckAndPlaceObjects();
            }
        }

        private bool ValidateAvatarRootPlacement()
        {
            try
            {
                // AvatarRootPathを取得
                string avatarRootPath = this.AvatarRootPath();

                if (string.IsNullOrEmpty(avatarRootPath))
                {
                    isValidPlacement = false;
                    errorMessage = "アバターの内部に配置してください。\nPlease place this inside the avatar.";
                    return false;
                }

                isValidPlacement = true;
                errorMessage = "このスクリプトは削除しないでください。";
                return true;
            }
            catch (System.Exception ex)
            {
                isValidPlacement = false;
                errorMessage = $"An error occurred while validating the avatar root: {ex.Message}";
                return false;
            }
        }

        private void CheckAndPlaceObjects()
        {
            if (targetPrefabs == null || targetPrefabs.Count == 0) return;

            Transform parentTransform = transform.parent;
            if (parentTransform == null) return; // アバター外ならここで打ち切り

            // 配置するプレハブを収集
            List<GameObject> prefabsToPlace = new List<GameObject>();

            foreach (GameObject prefab in targetPrefabs)
            {
                if (prefab == null) continue;

                bool objectExists = false;
                for (int i = 0; i < parentTransform.childCount; i++)
                {
                    Transform child = parentTransform.GetChild(i);
                    if (child == this.transform) continue;
                    if (child.name == prefab.name)
                    {
                        objectExists = true;
                        break;
                    }
                }

                if (!objectExists)
                {
                    prefabsToPlace.Add(prefab);
                }
            }

            // 一括でプレハブを配置（単一のUndo操作として）
            if (prefabsToPlace.Count > 0)
            {
                PlaceNewObjects(prefabsToPlace);
            }
        }

        private void PlaceNewObjects(List<GameObject> prefabsToPlace)
        {
            if (prefabsToPlace == null || prefabsToPlace.Count == 0) return;
            if (transform.parent == null) return;

            // 単一のUndo操作として全てのプレハブを配置
            UnityEditor.Undo.IncrementCurrentGroup();
            int group = UnityEditor.Undo.GetCurrentGroup();

            List<GameObject> createdObjects = new List<GameObject>();

            foreach (GameObject prefab in prefabsToPlace)
            {
                if (prefab == null) continue;

                GameObject newObject = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform.parent) as GameObject;

                if (newObject != null)
                {
                    newObject.transform.localPosition = Vector3.zero;
                    newObject.transform.localRotation = Quaternion.identity;
                    newObject.transform.localScale = Vector3.one;

                    createdObjects.Add(newObject);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Auto Place Objects");
                }
            }

            // 全ての操作を単一のUndo操作にまとめる
            UnityEditor.Undo.CollapseUndoOperations(group);

            if (createdObjects.Count > 0)
            {
                Debug.Log($"AutoAssetPlacer: {createdObjects.Count}個のオブジェクトを自動配置しました。");
            }
        }

        // 重複を除去するメソッド
        private void RemoveDuplicatesFromList()
        {
            if (targetPrefabs == null) return;

            // nullを除去してから重複を除去
            var uniquePrefabs = targetPrefabs
                .Where(prefab => prefab != null)
                .Distinct()
                .ToList();

            if (uniquePrefabs.Count != targetPrefabs.Count)
            {
                targetPrefabs = uniquePrefabs;
            }
        }

        // エディタ用のセットアップメソッド
        public void AddTargetPrefab(GameObject prefab)
        {
            if (prefab == null) return;
            if (targetPrefabs == null) targetPrefabs = new List<GameObject>();

            // 重複チェック
            if (!targetPrefabs.Contains(prefab))
            {
                targetPrefabs.Add(prefab);
            }
        }

        public void RemoveTargetPrefab(GameObject prefab)
        {
            if (targetPrefabs == null) return;
            targetPrefabs.Remove(prefab);
        }

        public void ClearTargetPrefabs()
        {
            if (targetPrefabs == null) targetPrefabs = new List<GameObject>();
            else targetPrefabs.Clear();
        }

        public bool IsValidPlacement()
        {
            ValidateAvatarRootPlacement();
            return isValidPlacement;
        }

        public string GetErrorMessage()
        {
            ValidateAvatarRootPlacement();
            return errorMessage;
        }
#endif
    }
}