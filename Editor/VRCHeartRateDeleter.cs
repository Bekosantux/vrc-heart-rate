#if UNITY_EDITOR
using BekoShop.VRCHeartRate;
using nadena.dev.ndmf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: ExportsPlugin(typeof(VRCHeartRateDeleter))]

namespace BekoShop.VRCHeartRate
{
    public class VRCHeartRateDeleter : Plugin<VRCHeartRateDeleter>
    {
        public override string QualifiedName => "beko.ooo.vrc-heart-rate.auto-module-placer-deleter";

        public override string DisplayName => "Auto Module Placer Deleter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Delete Script", ctx =>
                {
                    AutoModulePlacer[] placers = ctx.AvatarRootTransform.GetComponentsInChildren<AutoModulePlacer>(true);
                    Debug.Log($"[VRCHeartRateDeleter] Deleting {placers.Length} AutoModulePlacer scripts.");
                    foreach (var script in placers)
                    {
                        Object.DestroyImmediate(script);
                    }

                    VRCHeartRateModule[] modules = ctx.AvatarRootTransform.GetComponentsInChildren<VRCHeartRateModule>(true);
                    Debug.Log($"[VRCHeartRateDeleter] Deleting {modules.Length} VRCHeartRateModule scripts.");
                    foreach (var module in modules)
                    {
                        Object.DestroyImmediate(module);
                    }

                    VRCHeartRateModuleManual[] moduleManuals = ctx.AvatarRootTransform.GetComponentsInChildren<VRCHeartRateModuleManual>(true);
                    Debug.Log($"[VRCHeartRateDeleter] Deleting {moduleManuals.Length} VRCHeartRateModuleManual scripts.");
                    foreach (var moduleManual in moduleManuals)
                    {
                        Object.DestroyImmediate(moduleManual);
                    }
                }
            );
        }
    }
}
#endif