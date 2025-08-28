
using UnityEditor;
using UnityEngine;
using static BekoShop.VRCHeartRate.LocalizationManager;
using nadena.dev.ndmf.localization;

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(VRCHeartRateModuleManual))]
    public class OSCHeartRateModuleManualEditor : Editor
    {
        #if UNITY_EDITOR
        private bool showGameObjectSettings = false;

        public override void OnInspectorGUI()
        {
            var module = (VRCHeartRateModuleManual)target;

            EditorGUILayout.HelpBox(S("oscmodule_manual.message"), MessageType.Info);

            EditorGUILayout.Space();

            ShowLanguageUI();
        }
#endif
    }
}