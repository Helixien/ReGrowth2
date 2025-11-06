using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Plant), "GrowthRateFactor_Light", MethodType.Getter)]
    public static class Plant_GrowthRateFactor_Light_Patch
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            PlantExtension extension = __instance.def.GetModExtension<PlantExtension>();
            if (extension != null && extension.ignoresLightForGrow)
            {
                __result = 1f;
            }
        }
    }
}
