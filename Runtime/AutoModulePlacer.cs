using UnityEngine;
using System.Collections.Generic;
using VRC.SDKBase;

#if UNITY_EDITOR
using UnityEditor;
using nadena.dev.ndmf.util;
#endif

namespace BekoShop.VRCHeartRate
{
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

        public static readonly string[] OptionLabels = new[]
        {
            "FX","Additive","Action","Gesture","Base","Sitting","TPose","IKPose"
        };

        [Header("Prefabs")]
        [SerializeField, Tooltip("アバター内に1つだけ配置する親プレハブ（必須）")]
        private GameObject parentContainerPrefab;

        [SerializeField, Tooltip("各オプションに対応する子プレハブ")]
        private GameObject[] optionPrefabs = new GameObject[8];

        [Header("Options")]
        [SerializeField, Tooltip("各オプションを配置するかどうか")]
        private bool[] optionEnabled = new bool[8];

        // 状態
        private bool _isValidPlacement = false;
        private int _lastValidationFrame = -1;

        // Undo時の自動再配置抑制
        private static bool suppressAutoPlacement = false;

        private bool _placementScheduled;
        // OnValidate が呼ばれた時点の Undo グループ
        private int _validateUndoGroup = -1;

        private void OnEnable()
        {
            if (!gameObject.scene.IsValid()) return;

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            ValidateAndProcess();
        }

        private void OnDisable()
        {
            if (!gameObject.scene.IsValid()) return;

            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnUndoRedo()
        {
            // 予約していた遅延呼び出しをキャンセル
            if (_placementScheduled)
            {
                EditorApplication.delayCall -= DelayedValidateAndProcess;
                _placementScheduled = false;
            }

            suppressAutoPlacement = true;
            ValidateAndProcess();        // ここでは再配置しない
            suppressAutoPlacement = false;
        }

        private void OnHierarchyChanged()
        {
            EnsureValidPlacement();
        }

        private void OnValidate()
        {
            if (EditorApplication.isPlaying) return;
            if (BuildPipeline.isBuildingPlayer) return;
            if (!gameObject.scene.IsValid()) return;

            // 今の Undo グループ ID を記録しておく
            _validateUndoGroup = Undo.GetCurrentGroup();

            // 既に予約済みなら重複予約しない
            if (!_placementScheduled)
            {
                _placementScheduled = true;
                EditorApplication.delayCall += DelayedValidateAndProcess;
            }
        }

        public void ValidateAndProcess()
        {
            if (!EnsureValidPlacement()) return;
            if (suppressAutoPlacement) return;
            if (EditorApplication.isPlaying) return;
            if (BuildPipeline.isBuildingPlayer) return; // ビルド時は実行しない

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
            if (BuildPipeline.isBuildingPlayer) return; // ビルド時は実行しない

            // 新しいグループを開く
            Undo.IncrementCurrentGroup();
            int myGroup = Undo.GetCurrentGroup();

            // ここで階層を書き換える
            PlaceParentAndOptionsIfNeeded();

            /* 重要！
               OnValidate 時に取得したグループと結合して
               「ユーザ操作＋自動配置」を１つの操作にまとめる */
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
        public void SetOptionPrefab(OptionSlot slot, GameObject prefab) => optionPrefabs[(int)slot] = prefab;

        public bool[] GetOptionEnabled() => optionEnabled;
        public void SetOptionEnabled(OptionSlot slot, bool enabled) => optionEnabled[(int)slot] = enabled;

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
                    return _isValidPlacement;
                }

                _isValidPlacement = true;
                return _isValidPlacement;
            }
            catch (System.Exception)
            {
                _isValidPlacement = false;
                return _isValidPlacement;
            }
        }

        private void PlaceParentAndOptionsIfNeeded()
        {
            if (parentContainerPrefab == null) return;

            // アバタールートの取得
            Transform avatarRoot = GetAvatarRootTransform();
            if (avatarRoot == null) 
            {
                Debug.LogWarning("AutoAssetPlacer: Avatar root not found. Script must be placed inside an avatar hierarchy.", this);
                return; // アバタールートが見つからない場合は処理を中止
            }

            // 既存の親プレハブをアバタールートから GUID で検索（幅優先探索）
            string wantedGuid = GetPrefabAssetGUID(parentContainerPrefab);
            Transform parentNode = FindPrefabInstanceByGUID(avatarRoot, wantedGuid);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            bool isNewParent = false;

            // 親が存在しない場合のみ新規生成（スクリプトと同階層に配置）
            if (parentNode == null)
            {
                Transform siblingsParent = transform.parent;
                if (siblingsParent != null)
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
                        isNewParent = true;

                        // ログのContextを生成されたオブジェクト自身に変更
                        Debug.Log($"AutoAssetPlacer: Created new parent container prefab '{parentGO.name}'.", parentGO);
                    }
                }
            }
            else
            {
                // 既存の親プレハブを使用する場合のログ（Contextを既存オブジェクトに変更）
                Debug.Log($"AutoAssetPlacer: Using existing parent container prefab '{parentNode.name}'.", parentNode.gameObject);
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
                            createdChildren.Add(childGO);
                        }
                    }
                }

                if (createdCount > 0)
                {
                    string parentType = isNewParent ? "new" : "existing";
                    Debug.Log($"AutoAssetPlacer: Added {createdCount} option prefab(s) to {parentType} parent container.", parentNode.gameObject);

                    // 各子オブジェクトにも個別ログ（Contextを各子オブジェクトに設定）
                    foreach (var child in createdChildren)
                    {
                        Debug.Log($"AutoAssetPlacer: Added option prefab '{child.name}'.", child);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        /// AvatarRootPath() を使ってアバタールートのTransformを取得
        /// this.AvatarRootPath() はスクリプトプレハブのアバタールートからの相対パスを返すため、
        /// 実際のアバタールートオブジェクトを見つけて返す
        /// </summary>
        private Transform GetAvatarRootTransform()
        {
            try
            {
                string avatarRootPath = this.AvatarRootPath();
                if (string.IsNullOrEmpty(avatarRootPath))
                    return null;

                // スクリプトプレハブから親を辿ってアバタールートを見つける
                Transform current = transform;

                // AvatarRootPath() で取得したパスの階層数分だけ親を辿る
                // パスが "/" で始まる場合は除去
                if (avatarRootPath.StartsWith("/"))
                    avatarRootPath = avatarRootPath.Substring(1);

                string[] pathSegments = avatarRootPath.Split('/');

                // パスの階層数分だけ親を辿る（自分自身を含む）
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    if (current.parent == null)
                        return null; // 親がない場合は失敗
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

                // currentがプレハブインスタンスのルートかチェック
                var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(current.gameObject);
                if (prefabRoot != null && prefabRoot == current.gameObject)
                {
                    // プレハブアセットのGUIDを取得
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject);
                    if (!string.IsNullOrEmpty(path))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (guid == wantedGuid)
                        {
                            return current;
                        }
                    }
                }

                // 子要素をキューに追加
                for (int i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }

            return null;
        }
#endif
    }
}