using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
{
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
})]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        private static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            if (___pawn.IsBathingNow() &&
                ((___pawn.Position.GetTerrain(___pawn.Map).IsHotSpring() && hediff.def == HediffDefOf.Hypothermia)
                || (___pawn.Position.GetTerrain(___pawn.Map).IsWater && hediff.def == HediffDefOf.Heatstroke)))
            {
                return false;
            }
            return true;
        }
    }
}
