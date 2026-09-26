using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Mineable), nameof(Mineable.ExposeData))]
    public static class Mineable_ExposeData_Patch
    {
        public static void Postfix(Mineable __instance)
        {
            bool isSpaceRock = __instance.IsSpaceRock();
            Scribe_Values.Look(ref isSpaceRock, "isSpaceRock");
            if (isSpaceRock)
            {
                __instance.MarkAsSpaceRock();
            }
        }
    }
}
