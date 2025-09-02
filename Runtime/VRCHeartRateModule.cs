using UnityEngine;
using VRC.SDKBase;

namespace BekoShop.VRCHeartRate
{
    /// <summary>
    /// OSC心拍計モジュールの設定を管理するコンポーネント
    /// 実行時の動作には一切関与しません（VRChatビルド時には除去されます）
    /// </summary>
    [HelpURL("https://bekosantux.github.io/shop-document/category/vrc-heart-rate/")]
    public class VRCHeartRateModule : MonoBehaviour, IEditorOnly
    {
#if UNITY_EDITOR
        [Header("Heart Rate Control Settings")]
        [SerializeField, Tooltip("心拍数手動制御機能を削除してパラメータ数を削減します")]
        private bool removeManualControl = false;

        [Header("GameObject References")]
        [SerializeField, Tooltip("デフォルトパラメータ用ゲームオブジェクト")]
        private GameObject manualControlObject;

        [SerializeField, Tooltip("削減版パラメータ用ゲームオブジェクト")]
        private GameObject autoControlObject;

        [SerializeField, Tooltip("メニューオブジェクト")]
        private GameObject additionalSettingsObject;

        // 外部アクセス用プロパティ（Editor専用）
        public bool RemoveManualControl
        {
            get => removeManualControl;
            set
            {
                if (removeManualControl != value)
                {
                    removeManualControl = value;
                    // チェックボックスの状態変更時のみ処理を実行
                    UpdateGameObjectStates();
                }
            }
        }

        public GameObject ManualControlObject
        {
            get => manualControlObject;
            set => manualControlObject = value;
        }

        public GameObject AutoControlObject
        {
            get => autoControlObject;
            set => autoControlObject = value;
        }

        public GameObject AdditionalSettingsObject
        {
            get => additionalSettingsObject;
            set => additionalSettingsObject = value;
        }

        /// <summary>
        /// チェックボックスの状態に応じてゲームオブジェクトの有効状態を更新
        /// </summary>
        public void UpdateGameObjectStates()
        {
            if (removeManualControl)
            {
                // 手動制御を削除する場合
                SetGameObjectState(manualControlObject, false, true);  // Disable + EditorOnly
                SetGameObjectState(autoControlObject, true, false);    // Enable + Default
                SetGameObjectState(additionalSettingsObject, false, true); // Disable + EditorOnly
            }
            else
            {
                // 手動制御を維持する場合
                SetGameObjectState(manualControlObject, true, false);  // Enable + Default
                SetGameObjectState(autoControlObject, false, true);   // Disable + EditorOnly
                SetGameObjectState(additionalSettingsObject, true, false); // Enable + Default
            }
        }

        /// <summary>
        /// ゲームオブジェクトの有効状態とタグを設定
        /// </summary>
        private void SetGameObjectState(GameObject target, bool isActive, bool isEditorOnly)
        {
            if (target == null) return;

            target.SetActive(isActive);

            if (isEditorOnly)
            {
                target.tag = "EditorOnly";
            }
            else
            {
                // デフォルトタグに戻す
                target.tag = "Untagged";
            }
        }
#endif
    }
}