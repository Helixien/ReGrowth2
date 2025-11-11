using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Building), nameof(Building.DeSpawn))]
    public static class Building_DeSpawn_Patch
    {
        public static void Prefix(Building __instance, DestroyMode mode)
        {
            if (__instance is Mineable mineable)
            {
                mineable.UnmarkSpaceRock();
            }
            if (__instance.def.IsWall)
            {
                var mapComp = __instance.Map.GetComponent<WallSnowManager>();
                if (mapComp.snowCoveredWalls.Contains(__instance))
                {
                    mapComp.snowCoveredWalls.Remove(__instance);
                    __instance.Map.mapDrawer.MapMeshDirty(__instance.Position, MapMeshFlagDefOf.Things);
                }
            }
        }
    }
}
