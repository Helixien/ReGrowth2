using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PollutionGrid), "SetPolluted")]
    public static class PollutionGrid_SetPolluted_Patch
    {
        public static void Postfix(PollutionGrid __instance, IntVec3 cell)
        {
            if (cell.InBounds(__instance.map))
            {
                __instance.map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things, regenAdjacentCells: true, regenAdjacentSections: true);
                __instance.map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Buildings, regenAdjacentCells: true, regenAdjacentSections: true);
            }
        }
    }
}

