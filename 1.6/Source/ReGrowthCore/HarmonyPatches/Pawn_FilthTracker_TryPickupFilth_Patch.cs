using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Pawn_FilthTracker), "TryPickupFilth")]
    public static class Pawn_FilthTracker_TryPickupFilth_Patch
    {
        public static void Prefix(Pawn_FilthTracker __instance, out (TerrainDef def, ThingDef filth) __state)
        {
            TerrainDef terrDef = __instance.pawn.Map.terrainGrid.TerrainAt(__instance.pawn.Position);
            __state = (terrDef, terrDef.generatedFilth);
            if (terrDef.TryGetBiomeSpecificTerrain(__instance.pawn.Map, out var biomeTerrain)
                && biomeTerrain.generatedFilth != null)
            {
                terrDef.generatedFilth = biomeTerrain.generatedFilth;
            }
        }

        public static void Postfix((TerrainDef def, ThingDef filth) __state)
        {
            __state.def.generatedFilth = __state.filth;
        }
    }
}

