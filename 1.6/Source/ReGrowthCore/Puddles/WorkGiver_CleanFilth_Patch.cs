using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WorkGiver_CleanFilth), "HasJobOnThing")]
    public static class WorkGiver_CleanFilth_HasJobOnThing_Patch
    {
        public static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            if (__result && t != null)
            {
                if (t.def == RG_DefOf.RG_FilthWater || t.def == RG_DefOf.RG_FilthWaterSpatter)
                {
                    __result = false;
                }
            }
        }
    }
}
