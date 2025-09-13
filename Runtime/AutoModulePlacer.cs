using UnityEngine;
using System.Collections.Generic;
using VRC.SDKBase;

#if UNITY_EDITOR
using UnityEditor;
using nadena.dev.ndmf.util;
#endif

namespace BekoShop.VRCHeartRate
{
    [HelpURL("https://bekosantux.github.io/shop-document/category/vrc-heart-rate/")]
    public class AutoModulePlacer : MonoBehaviour, IEditorOnly
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

        private const int OptionCount = 8;

        public static readonly string[] OptionLabels = new[]
        {
            "FX","Additive","Action","Gesture","Base","Sitting","TPose","IKPose"
        };

        [Header("Prefabs")]
        [SerializeField, Tooltip("アバター内に1つだけ配置する親プレハブ（必須）")]
        private GameObject parentContainerPrefab;

        [SerializeField, Tooltip("各オプションに対応する子プレハブ")]
        private GameObject[] optionPrefabs = new GameObject[OptionCount];

        [Header("Options")]
        [SerializeField, Tooltip("各オプションを配置するかどうか")]
        private bool[] optionEnabled = new bool[OptionCount];

        // 状態
        private bool _isValidPlacement;
        private int _lastValidationFrame = -1;

        // Undo時の自動再配置抑制（静的：他インスタンスとも共有）
        private static bool suppressAutoPlacement = false;

        // 遅延処理制御
        private bool _placementScheduled;
        // OnValidate 時点の Undo グループ
        private int _validateUndoGroup = -1;

        private void OnEnable()
        {
            if (!gameObject.scene.IsValid()) return;

            EnsureArrays();

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            ValidateAndProcess();
        }

        private void OnDisable()
        {
            if (!gameObject.scene.IsValid()) return;

            CancelScheduledProcess();

            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnUndoRedo()
        {
            CancelScheduledProcess();

            suppressAutoPlacement = true;
            // 配置の妥当性だけは更新（再配置は行わない）
            EnsureValidPlacement();
            suppressAutoPlacement = false;
        }

        private void OnHierarchyChanged()
        {
            EnsureValidPlacement();
        }

        private void OnValidate()
        {
            if (!CanRunEditorAutomation()) return;
            if (!gameObject.scene.IsValid()) return;

            EnsureArrays();

            // 今の Undo グループ ID を記録
            _validateUndoGroup = Undo.GetCurrentGroup();

            // 既に予約済みなら重複予約しない
            ScheduleDelayedValidateAndProcess();
        }

        public void ValidateAndProcess()
        {
            if (!EnsureValidPlacement()) return;
            if (suppressAutoPlacement) return;
            if (!CanRunEditorAutomation()) return;

            PlaceParentAndOptionsIfNeeded();
        }

        private void DelayedValidateAndProcess()
        {
            // 予約を解除
            EditorApplication.delayCall -= DelayedValidateAndProcess;
            _placementScheduled = false;

            // オブジェクトが消えていたら何もしない
            if (this == null) return;
            if (suppressAutoPlacement) return;
            if (BuildPipeline.isBuildingPlayer) return;

            // 新しいグループを開く
            Undo.IncrementCurrentGroup();
            int myGroup = Undo.GetCurrentGroup();

            // 階層を書き換える
            PlaceParentAndOptionsIfNeeded();

            // OnValidate 時に取得したグループと結合して1操作にまとめる
            if (_validateUndoGroup >= 0)
            {
                Undo.CollapseUndoOperations(_validateUndoGroup);
                _validateUndoGroup = -1;
            }
            else
            {
                Undo.CollapseUndoOperations(myGroup);
            }
        }

        // 外部アクセス（Editor用）
        public GameObject GetParentContainerPrefab() => parentContainerPrefab;
        public void SetParentContainerPrefab(GameObject prefab) => parentContainerPrefab = prefab;

        public GameObject[] GetOptionPrefabs() => optionPrefabs;
        public void SetOptionPrefab(OptionSlot slot, GameObject prefab)
        {
            EnsureArrays();
            optionPrefabs[(int)slot] = prefab;
        }

        public bool[] GetOptionEnabled() => optionEnabled;
        public void SetOptionEnabled(OptionSlot slot, bool enabled)
        {
            EnsureArrays();
            optionEnabled[(int)slot] = enabled;
        }

        public bool IsValidPlacement() => EnsureValidPlacement();

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
            EnsureArrays();
            for (int i = 0; i < OptionCount; i++)
            {
                if (optionEnabled[i]) yield return i;
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
                _isValidPlacement = !string.IsNullOrEmpty(avatarRootPath);
            }
            catch
            {
                _isValidPlacement = false;
            }
            return _isValidPlacement;
        }

        private void PlaceParentAndOptionsIfNeeded()
        {
            if (parentContainerPrefab == null) return;

            // アバタールートの取得
            Transform avatarRoot = GetAvatarRootTransform();
            if (avatarRoot == null)
            {
                Debug.LogWarning("AutoModulePlacer: Avatar root not found. Script must be placed inside an avatar hierarchy.", this);
                return;
            }

            // 既存の親プレハブをアバタールートから GUID で検索（幅優先探索）
            string wantedGuid = GetPrefabAssetGUID(parentContainerPrefab);
            Transform parentNode = FindPrefabInstanceByGUID(avatarRoot, wantedGuid);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            bool createdParent = false;

            // 親が存在しない場合のみ新規生成（スクリプトと同階層に配置）
            if (parentNode == null)
            {
                Transform siblingsParent = transform.parent;
                if (siblingsParent != null)
                {
                    var parentGO = PrefabUtility.InstantiatePrefab(parentContainerPrefab) as GameObject;
                    if (parentGO != null)
                    {
                        parentGO.transform.SetParent(siblingsParent);
                        parentGO.transform.localPosition = Vector3.zero;
                        parentGO.transform.localRotation = Quaternion.identity;
                        parentGO.transform.localScale = Vector3.one;
                        Undo.RegisterCreatedObjectUndo(parentGO, "Auto Place Parent Container");
                        parentNode = parentGO.transform;
                        createdParent = true;

                        Debug.Log($"AutoModulePlacer: Created new parent container prefab '{parentGO.name}'.", parentGO);
                    }
                }
            }

            // 親が存在するなら子の不足分を配置
            if (parentNode != null)
            {
                int createdCount = 0;
                List<GameObject> createdChildren = new List<GameObject>();

                foreach (int slot in EnabledSlots())
                {
                    var childPrefab = optionPrefabs[slot];
                    if (childPrefab == null) continue;

                    if (!ChildExistsByName(parentNode, childPrefab.name))
                    {
                        var childGO = PrefabUtility.InstantiatePrefab(childPrefab) as GameObject;
                        if (childGO != null)
                        {
                            childGO.transform.SetParent(parentNode);
                            childGO.transform.localPosition = Vector3.zero;
                            childGO.transform.localRotation = Quaternion.identity;
                            childGO.transform.localScale = Vector3.one;
                            Undo.RegisterCreatedObjectUndo(childGO, $"Auto Place Option {OptionLabels[slot]}");
                            createdCount++;
                            createdChildren.Add(childGO);
                        }
                    }
                }

                if (createdCount > 0)
                {
                    string parentType = createdParent ? "new" : "existing";
                    Debug.Log($"AutoModulePlacer: Added {createdCount} option prefab(s) to {parentType} parent container.", parentNode.gameObject);

                    foreach (var child in createdChildren)
                    {
                        Debug.Log($"AutoModulePlacer: Added option prefab '{child.name}'.", child);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        /// AvatarRootPath() を使ってアバタールートのTransformを取得
        /// </summary>
        private Transform GetAvatarRootTransform()
        {
            try
            {
                string avatarRootPath = this.AvatarRootPath();
                if (string.IsNullOrEmpty(avatarRootPath)) return null;

                Transform current = transform;

                if (avatarRootPath.StartsWith("/"))
                    avatarRootPath = avatarRootPath.Substring(1);

                string[] pathSegments = avatarRootPath.Split('/');
                // パスの階層数分だけ親を辿る（自分自身を含む）
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    if (current == null || current.parent == null) return null;
                    current = current.parent;
                }

                return current;
            }
            catch
            {
                return null;
            }
        }

        private static string GetPrefabAssetGUID(GameObject prefabAsset)
        {
            if (prefabAsset == null) return null;
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.AssetPathToGUID(path);
        }

        /// <summary>
        /// 指定されたルートから幅優先でプレハブインスタンスを探索し、GUIDが一致するものを返す
        /// </summary>
        private static Transform FindPrefabInstanceByGUID(Transform root, string wantedGuid)
        {
            if (root == null || string.IsNullOrEmpty(wantedGuid)) return null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // current がプレハブインスタンスのルートかチェック
                var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(current.gameObject);
                if (instanceRoot != null && instanceRoot == current.gameObject)
                {
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject);
                    if (!string.IsNullOrEmpty(path))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (guid == wantedGuid) return current;
                    }
                }

                for (int i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }

            return null;
        }

        private static bool ChildExistsByName(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName) return true;
            }
            return false;
        }

        private bool CanRunEditorAutomation()
        {
            if (EditorApplication.isPlaying) return false;
            if (BuildPipeline.isBuildingPlayer) return false;
            return true;
        }

        private void ScheduleDelayedValidateAndProcess()
        {
            if (_placementScheduled) return;
            _placementScheduled = true;
            EditorApplication.delayCall += DelayedValidateAndProcess;
        }

        private void CancelScheduledProcess()
        {
            if (!_placementScheduled) return;
            EditorApplication.delayCall -= DelayedValidateAndProcess;
            _placementScheduled = false;
        }

        private void EnsureArrays()
        {
            if (optionPrefabs == null || optionPrefabs.Length != OptionCount)
            {
                var old = optionPrefabs;
                optionPrefabs = new GameObject[OptionCount];
                if (old != null)
                {
                    int copy = Mathf.Min(OptionCount, old.Length);
                    for (int i = 0; i < copy; i++) optionPrefabs[i] = old[i];
                }
            }

            if (optionEnabled == null || optionEnabled.Length != OptionCount)
            {
                var old = optionEnabled;
                optionEnabled = new bool[OptionCount];
                if (old != null)
                {
                    int copy = Mathf.Min(OptionCount, old.Length);
                    for (int i = 0; i < copy; i++) optionEnabled[i] = old[i];
                }
            }
        }
#endif
    }
}
