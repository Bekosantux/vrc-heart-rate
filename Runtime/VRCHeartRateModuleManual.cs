using UnityEngine;
using VRC.SDKBase;

namespace BekoShop.VRCHeartRate
{
    /// <summary>
    /// OSC心拍計モジュールの設定を管理するコンポーネント
    /// 実行時の動作には一切関与しません（VRChatビルド時には除去されます）
    /// </summary>
    [HelpURL("https://bekosantux.github.io/shop-document/category/vrc-heart-rate/")]
    public class VRCHeartRateModuleManual : MonoBehaviour, IEditorOnly
    {
#if UNITY_EDITOR

#endif
    }
}