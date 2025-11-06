using HarmonyLib;
using RimWorld;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PlantFallColors), "GetFallColorFactor")]
    public static class PlantFallColors_GetFallColorFactor_Patch
    {
        public static float fallColorFactor = 0f;
        public static void Postfix(ref float __result)
        {
            fallColorFactor = __result;
        }
    }
}
