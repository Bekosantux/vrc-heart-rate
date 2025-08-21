using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using nadena.dev.ndmf.util;
using UnityEditor;
#endif

namespace BekoShop.VRCHeartRate
{
    [ExecuteAlways]
    public class AutoAssetPlacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
#if UNITY_EDITOR
        // 定数定義
        private const string MESSAGE_DELETE_WARNING = "このスクリプトは削除しないでください。\nDon't delete this script.";
        private const string MESSAGE_OUTSIDE_AVATAR = "アバターの内部に配置してください。\nPlace this inside the avatar.";

        [SerializeField, HideInInspector]
        private List<GameObject> _targetPrefabs = new List<GameObject>();

        // public プロパティは直接フィールドにする（パフォーマンス向上）
        public List<GameObject> targetPrefabs => _targetPrefabs;

        // キャッシュするためreadonly
        private bool _isValidPlacement = false;
        private string _errorMessage = "";

        // 状態キャッシュの有効期限
        private int _lastValidationFrame = -1;

        // Undo/Redoで自動配置しないように状態を保持
        private static bool suppressAutoPlacement = false;

        private void OnEnable()
        {
            // イベントを一度クリアしてから登録（重複登録防止）
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;

            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;

            // 初回検証
            ValidateAndProcess();
        }

        private void OnDisable()
        {
            // イベント解除を忘れない
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnUndoRedo()
        {
            suppressAutoPlacement = true;
            ValidateAndProcess();
            suppressAutoPlacement = false;
        }

        private void OnHierarchyChanged()
        {
            // 検証のみ行い、結果をキャッシュ（_lastValidationFrameを更新）
            ValidateAvatarRootPlacement();

            // 必要なビューのみ再描画
            EditorUtility.SetDirty(this);
        }

        private void Start()
        {
            if (!Application.isPlaying) ValidateAndProcess();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // 遅延実行で警告を出さないようにする
            EditorApplication.delayCall += _OnValidate;
        }

        private void _OnValidate()
        {
            EditorApplication.delayCall -= _OnValidate;

            RemoveDuplicatesFromList();
            ValidateAndProcess();
        }

        public void ValidateAndProcess()
        {
            if (!EnsureValidPlacement()) return;

            if (!suppressAutoPlacement)
            {
                CheckAndPlaceObjects();
            }
        }

        /// <summary>
        /// アバターRoot検証を行い結果をキャッシュします
        /// </summary>
        private bool EnsureValidPlacement()
        {
            // 同一フレーム内では再検証しない（パフォーマンス向上）
            if (_lastValidationFrame == Time.frameCount)
                return _isValidPlacement;

            return ValidateAvatarRootPlacement();
        }

        private bool ValidateAvatarRootPlacement()
        {
            _lastValidationFrame = Time.frameCount;

            try
            {
                string avatarRootPath = this.AvatarRootPath();

                if (string.IsNullOrEmpty(avatarRootPath))
                {
                    _isValidPlacement = false;
                    _errorMessage = MESSAGE_OUTSIDE_AVATAR;
                    return false;
                }

                _isValidPlacement = true;
                _errorMessage = MESSAGE_DELETE_WARNING;
                return true;
            }
            catch (System.Exception ex)
            {
                _isValidPlacement = false;
                _errorMessage = $"Error validating avatar root: {ex.Message}";
                return false;
            }
        }

        private void CheckAndPlaceObjects()
        {
            if (_targetPrefabs == null || _targetPrefabs.Count == 0) return;

            Transform parentTransform = transform.parent;
            if (parentTransform == null) return;

            // 事前に配列サイズを確保して GC Alloc を減らす
            List<GameObject> prefabsToPlace = new List<GameObject>(_targetPrefabs.Count);

            foreach (GameObject prefab in _targetPrefabs)
            {
                if (prefab == null) continue;

                bool objectExists = false;
                int childCount = parentTransform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = parentTransform.GetChild(i);
                    if (child == transform) continue;
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

            if (prefabsToPlace.Count > 0)
            {
                PlaceNewObjects(prefabsToPlace);
            }
        }

        private void PlaceNewObjects(List<GameObject> prefabsToPlace)
        {
            if (prefabsToPlace.Count == 0) return;

            // 単一の Undo グループで処理
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            int createdCount = 0;
            Transform parent = transform.parent;

            foreach (GameObject prefab in prefabsToPlace)
            {
                if (prefab == null) continue;

                GameObject newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (newObject == null) continue;

                if (parent != null)
                {
                    newObject.transform.SetParent(parent);
                }

                newObject.transform.localPosition = Vector3.zero;
                newObject.transform.localRotation = Quaternion.identity;
                newObject.transform.localScale = Vector3.one;

                Undo.RegisterCreatedObjectUndo(newObject, "Auto Place Objects");
                createdCount++;
            }

            Undo.CollapseUndoOperations(group);

            if (createdCount > 0)
            {
                Debug.Log($"AutoAssetPlacer: {createdCount}個のオブジェクトを自動配置しました。");
            }
        }

        private void RemoveDuplicatesFromList()
        {
            if (_targetPrefabs == null) return;

            // 必要な場合のみ新しいリストを作成
            bool hasNull = false;
            bool hasDuplicates = false;

            // null チェックとユニーク性の確認
            var uniqueItems = new HashSet<GameObject>();
            foreach (var prefab in _targetPrefabs)
            {
                if (prefab == null)
                {
                    hasNull = true;
                }
                else if (!uniqueItems.Add(prefab))
                {
                    hasDuplicates = true;
                }

                // nullと重複の両方が見つかった場合早期終了
                if (hasNull && hasDuplicates) break;
            }

            // 重複またはnullがある場合のみリストを再構築
            if (hasNull || hasDuplicates)
            {
                _targetPrefabs = _targetPrefabs
                    .Where(prefab => prefab != null)
                    .Distinct()
                    .ToList();
            }
        }

        public bool IsValidPlacement() => EnsureValidPlacement();

        public string GetErrorMessage()
        {
            EnsureValidPlacement();
            return _errorMessage;
        }
#endif
    }
}