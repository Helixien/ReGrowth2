using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(DigUtility), nameof(DigUtility.PassBlockerJob))]
    public static class DigUtility_PassBlockerJob_Patch
    {
        public static void Prefix(ref bool canMineMineables, Thing blocker)
        {
            if (blocker != null && blocker.def.defName.StartsWith("RG_BoulderRock"))
            {
                canMineMineables = false;
            }
        }
    }
}
