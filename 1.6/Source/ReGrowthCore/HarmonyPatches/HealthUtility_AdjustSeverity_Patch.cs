using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(HealthUtility), "AdjustSeverity")]
    public static class HealthUtility_AdjustSeverity_Patch
    {
        private static bool Prefix(Pawn pawn, HediffDef hdDef, float sevOffset)
        {
            if (sevOffset > 0 && pawn.IsBathingNow() &&
                ((pawn.Position.GetTerrain(pawn.Map).IsHotSpring() && hdDef == HediffDefOf.Hypothermia)
                || (pawn.Position.GetTerrain(pawn.Map).IsWater && hdDef == HediffDefOf.Heatstroke)))
            {
                return false;
            }
            return true;
        }
    }
}
