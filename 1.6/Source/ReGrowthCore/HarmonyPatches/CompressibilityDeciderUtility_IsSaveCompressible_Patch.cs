using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
    public static class CompressibilityDeciderUtility_IsSaveCompressible_Patch
    {
        public static void Postfix(ref bool __result, Thing t)
        {
            if (__result && (t.def.HasModExtension<BoulderProperties>() ||
                (t is Mineable mineable && mineable.IsSpaceRock())))
            {
                __result = false;
            }
        }
    }
}
