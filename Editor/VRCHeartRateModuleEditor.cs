
using UnityEditor;
using UnityEngine;
using static BekoShop.VRCHeartRate.LocalizationManager;
using nadena.dev.ndmf.localization;

namespace BekoShop.VRCHeartRate
{
    [CustomEditor(typeof(VRCHeartRateModule))]
    public class OSCHeartRateModuleEditor : Editor
    {
        #if UNITY_EDITOR
        private bool showGameObjectSettings = false;

        public override void OnInspectorGUI()
        {
            var module = (VRCHeartRateModule)target;

            EditorGUILayout.HelpBox(S("oscmodule.message"), MessageType.Info);

            EditorGUILayout.Space();

            // 心拍数手動制御機能の削除設定
            EditorGUI.BeginChangeCheck();
            bool newRemoveManualControl = EditorGUILayout.ToggleLeft(
                S("oscmodule.checkbox.remove_manual"),
                module.RemoveManualControl
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Toggle Manual Control Removal");
                module.RemoveManualControl = newRemoveManualControl; // setterで処理
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space();

            // ゲームオブジェクト参照設定
            showGameObjectSettings = EditorGUILayout.Foldout(
                showGameObjectSettings,
                S("oscmodule.foldout.objects"),
                true
            );
            if (showGameObjectSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                var newManualControlObject = (GameObject)EditorGUILayout.ObjectField(
                    S("oscmodule.tooltip.manual"),
                    module.ManualControlObject,
                    typeof(GameObject),
                    true
                );

                var newAutoControlObject = (GameObject)EditorGUILayout.ObjectField(
                    S("oscmodule.tooltip.auto"),
                    module.AutoControlObject,
                    typeof(GameObject),
                    true
                );

                EditorGUILayout.Space();

                var newMenuObject = (GameObject)EditorGUILayout.ObjectField(
                    S("oscmodule.tooltip.additional"),
                    module.AdditionalSettingsObject,
                    typeof(GameObject),
                    true
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change GameObject References");
                    module.ManualControlObject = newManualControlObject;
                    module.AutoControlObject = newAutoControlObject;
                    module.AdditionalSettingsObject = newMenuObject;
                    EditorUtility.SetDirty(target);
                }

                EditorGUI.indentLevel--;

                // 現在の設定状態を表示
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(S("oscmodule.label.current"), EditorStyles.boldLabel);

                string statusText = module.RemoveManualControl
                    ? S("oscmodule.status.removed")
                    : S("oscmodule.status.enabled");
                EditorGUILayout.HelpBox(statusText, MessageType.None);

                // オブジェクトの状態を表示
                DisplayObjectStatus(S("oscmodule.status.manual"), module.ManualControlObject, !module.RemoveManualControl);
                DisplayObjectStatus(S("oscmodule.status.auto"), module.AutoControlObject, module.RemoveManualControl);
                DisplayObjectStatus(S("oscmodule.status.additional"), module.AdditionalSettingsObject, !module.RemoveManualControl);
            }

            ShowLanguageUI();
        }

        private void DisplayObjectStatus(string objectName, GameObject obj, bool shouldBeActive)
        {
            if (obj == null) return;
            string status = shouldBeActive ? S("oscmodule.status.active") : S("oscmodule.status.inactive");
            string tag = shouldBeActive ? S("oscmodule.status.default") : S("oscmodule.status.editoronly");
            EditorGUILayout.LabelField($"{objectName}: {status} ({tag})", EditorStyles.miniLabel);
        }
#endif
    }
}