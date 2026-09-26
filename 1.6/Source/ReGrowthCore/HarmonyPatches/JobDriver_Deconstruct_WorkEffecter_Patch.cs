using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(JobDriver_Deconstruct), "WorkEffecter", MethodType.Getter)]
    public static class JobDriver_Deconstruct_WorkEffecter_Patch
    {
        public static void Postfix(JobDriver_Deconstruct __instance, ref EffecterDef __result)
        {
            var extension = __instance.Building?.def.GetModExtension<ThingExtension>();
            if (extension?.deconstructEffecter != null)
            {
                __result = extension.deconstructEffecter;
            }
        }
    }
}
