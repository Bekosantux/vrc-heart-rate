using UnityEngine;
using UnityEditor;
using nadena.dev.ndmf.localization;

namespace BekoShop.VRCHeartRate
{
    [HelpURL("https://bekosantux.github.io/ShopDoc/VRCHeartRate")]
    public class OscHeartRateNotice : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
    }
}

#if UNITY_EDITOR

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(OscHeartRateNotice))]
    public class OscHeartRateNoticeEditor : Editor
    {
        private static readonly string JA_MESSAGE = "このプレハブは心拍数をOSCで送信して利用するアセットを機能させるもので、単体では意味がありません。\nOSC心拍計の機能を使用しない場合は削除することができます。";
        private static readonly string EN_MESSAGE = "This prefab enables assets that use heart rate transmitted via OSC and has no meaning on its own.\nIf you do not use the OSC heart rate feature, you can safely delete this prefab.";
        private static readonly string ZH_TW_MESSAGE = "此預製物用於啟用透過 OSC 傳送心率以供資產使用的功能，單獨存在並無意義。\n若不使用 OSC 心率功能，可以將其刪除。";
        private static readonly string ZH_CN_MESSAGE = "此预制体用于启用通过 OSC 发送心率供资产使用的功能，其本身没有意义。\n如果不使用 OSC 心率功能，可以将其删除。";
        private static readonly string KO_MESSAGE = "이 프리팹은 OSC로 전송된 심박수를 사용하는 애셋을 동작시키기 위한 것으로, 단독으로는 의미가 없습니다.\nOSC 심박수 기능을 사용하지 않는 경우 이 프리팹을 삭제해도 됩니다.";

        private void OnEnable()
        {
            // 言語変更時にInspectorを再描画
            LanguagePrefs.RegisterLanguageChangeCallback(this, _ => Repaint());
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(GetLocalizedMessage(), MessageType.Info);
        }

        private static string GetLocalizedMessage()
        {
            var lang = (LanguagePrefs.Language ?? string.Empty).ToLowerInvariant();

            if (lang.StartsWith("ja")) return JA_MESSAGE;
            if (lang.StartsWith("en")) return EN_MESSAGE;

            // 繁体 / 简体（地域コードや Hant/Hans 指定に対応）
            if (lang.StartsWith("zh-tw") || lang.Contains("hant")) return ZH_TW_MESSAGE;
            if (lang.StartsWith("zh-cn") || lang.Contains("hans")) return ZH_CN_MESSAGE;
            if (lang.StartsWith("zh")) return ZH_CN_MESSAGE; // zh のみは簡体にフォールバック

            if (lang.StartsWith("ko")) return KO_MESSAGE;

            // 未対応は英語へフォールバック
            return EN_MESSAGE;
        }
    }
}
#endif