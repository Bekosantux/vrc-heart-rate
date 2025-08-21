using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using nadena.dev.ndmf.util;
#endif

namespace BekoShop.VRCHeartRate
{
    // アバター内に親プレハブを1つ配置し、その子として選択されたオプションプレハブを不足分だけ配置する
    public class AutoAssetPlacer : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
#if UNITY_EDITOR
        public enum OptionSlot
        {
            FX = 0,
            Additive = 1,
            Action = 2,
            Gesture = 3,
            Base = 4,
            Sitting = 5,
            TPose = 6,
            IKPose = 7
        }

        // Editor から参照するため public
        public static readonly string[] OptionLabels = new[]
        {
            "FX","Additive","Action","Gesture","Base","Sitting","TPose","IKPose"
        };

        [Header("Prefabs")]
        [SerializeField, Tooltip("アバター内に1つだけ配置する親プレハブ（必須）")]
        private GameObject parentContainerPrefab;

        [SerializeField, Tooltip("各オプションに対応する子プレハブ（要素数8・インデックスは OptionSlot に対応）")]
        private GameObject[] optionPrefabs = new GameObject[8];

        [Header("Options")]
        [SerializeField, Tooltip("各オプションを配置するかどうか（要素数8・インデックスは OptionSlot に対応）")]
        private bool[] optionEnabled = new bool[8];

        // 状態
        private bool _isValidPlacement = false;
        private string _statusMessage = "";
        private int _lastValidationFrame = -1;

        // Undo時の自動再配置抑制
        private static bool suppressAutoPlacement = false;

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            ValidateAndProcess();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnUndoRedo()
        {
            suppressAutoPlacement = true;
            ValidateAndProcess();
            suppressAutoPlacement = false;
        }

        private void OnHierarchyChanged()
        {
            EnsureValidPlacement();
        }

        private void Start()
        {
            if (!Application.isPlaying) ValidateAndProcess();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            // 配列長は固定想定のため保証処理は行わない
            ValidateAndProcess();
        }

        public void ValidateAndProcess()
        {
            if (!EnsureValidPlacement()) return;
            if (suppressAutoPlacement) return;

            PlaceParentAndOptionsIfNeeded();
        }

        // 外部アクセス（Editor用）
        public GameObject GetParentContainerPrefab() => parentContainerPrefab;
        public void SetParentContainerPrefab(GameObject prefab) => parentContainerPrefab = prefab;

        public GameObject[] GetOptionPrefabs() => optionPrefabs;
        public void SetOptionPrefab(OptionSlot slot, GameObject prefab) => optionPrefabs[(int)slot] = prefab;

        public bool[] GetOptionEnabled() => optionEnabled;
        public void SetOptionEnabled(OptionSlot slot, bool enabled) => optionEnabled[(int)slot] = enabled;

        public bool IsValidPlacement() => EnsureValidPlacement();
        public string GetStatusMessage()
        {
            EnsureValidPlacement();
            return _statusMessage;
        }

        public bool HasAnyOptionEnabled()
        {
            if (optionEnabled == null) return false;
            for (int i = 0; i < optionEnabled.Length; i++)
            {
                if (optionEnabled[i]) return true;
            }
            return false;
        }

        public IEnumerable<int> EnabledSlots()
        {
            for (int i = 0; i < 8; i++)
            {
                if (optionEnabled != null && i < optionEnabled.Length && optionEnabled[i]) yield return i;
            }
        }

        // -------------------- 内部実装 --------------------

        private bool EnsureValidPlacement()
        {
            if (_lastValidationFrame == Time.frameCount) return _isValidPlacement;
            _lastValidationFrame = Time.frameCount;

            try
            {
                string avatarRootPath = this.AvatarRootPath();
                if (string.IsNullOrEmpty(avatarRootPath))
                {
                    _isValidPlacement = false;
                    _statusMessage = "アバターの内部に配置してください。\nPlease place this inside the avatar.";
                    return _isValidPlacement;
                }

                _isValidPlacement = true;
                _statusMessage = "このスクリプトは削除しないでください。\nPlease don't delete this script.";
                return _isValidPlacement;
            }
            catch (System.Exception ex)
            {
                _isValidPlacement = false;
                _statusMessage = $"Error validating avatar root: {ex.Message}";
                return _isValidPlacement;
            }
        }

        private void PlaceParentAndOptionsIfNeeded()
        {
            // 親プレハブ必須（フォールバック生成はしない）
            if (parentContainerPrefab == null) return;

            // 親はスクリプトプレハブと同階層（同じ親の子）に配置する
            Transform siblingsParent = transform.parent;
            if (siblingsParent == null) return;

            // 既存の親プレハブを GUID で検索（同階層内のみ）
            string wantedGuid = GetPrefabAssetGUID(parentContainerPrefab);
            Transform parentNode = FindSiblingPrefabInstanceByGUID(siblingsParent, wantedGuid);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            // 親が存在しない場合は生成
            if (parentNode == null)
            {
                GameObject parentGO = PrefabUtility.InstantiatePrefab(parentContainerPrefab) as GameObject;
                if (parentGO != null)
                {
                    parentGO.transform.SetParent(siblingsParent);
                    parentGO.transform.localPosition = Vector3.zero;
                    parentGO.transform.localRotation = Quaternion.identity;
                    parentGO.transform.localScale = Vector3.one;
                    Undo.RegisterCreatedObjectUndo(parentGO, "Auto Place Parent Container");
                    parentNode = parentGO.transform;
                }
            }

            // 親が存在するなら子の不足分を配置
            if (parentNode != null)
            {
                int createdCount = 0;

                foreach (int slot in EnabledSlots())
                {
                    var childPrefab = optionPrefabs[slot];
                    if (childPrefab == null) continue;

                    // 親の直下に同名オブジェクトが存在するか確認
                    bool exists = false;
                    for (int i = 0; i < parentNode.childCount; i++)
                    {
                        var child = parentNode.GetChild(i);
                        if (child.name == childPrefab.name)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        GameObject childGO = PrefabUtility.InstantiatePrefab(childPrefab) as GameObject;
                        if (childGO != null)
                        {
                            childGO.transform.SetParent(parentNode);
                            childGO.transform.localPosition = Vector3.zero;
                            childGO.transform.localRotation = Quaternion.identity;
                            childGO.transform.localScale = Vector3.one;
                            Undo.RegisterCreatedObjectUndo(childGO, $"Auto Place Option {OptionLabels[slot]}");
                            createdCount++;
                        }
                    }
                }

                if (createdCount > 0)
                {
                    Debug.Log($"AutoAssetPlacer: {createdCount} 個のオプションプレハブを自動配置しました。", this);
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        private static string GetPrefabAssetGUID(GameObject prefabAsset)
        {
            if (prefabAsset == null) return null;
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.AssetPathToGUID(path);
        }

        private static Transform FindSiblingPrefabInstanceByGUID(Transform siblingsParent, string wantedGuid)
        {
            if (siblingsParent == null || string.IsNullOrEmpty(wantedGuid)) return null;

            for (int i = 0; i < siblingsParent.childCount; i++)
            {
                var child = siblingsParent.GetChild(i);

                // この child がプレハブインスタンスのルートか確認
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
                if (root == null || root != child.gameObject) continue;

                // そのプレハブインスタンスの元アセットGUIDを取得
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                if (string.IsNullOrEmpty(path)) continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (guid == wantedGuid)
                {
                    return child;
                }
            }
            return null;
        }
#endif
    }
}