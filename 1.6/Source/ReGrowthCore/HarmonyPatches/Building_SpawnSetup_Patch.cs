using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    public static class Building_SpawnSetup_Patch
    {
        public static void Postfix(Building __instance)
        {
            if (__instance is Mineable mineable && mineable.IsSpaceRock())
            {
                mineable.UpdateGraphic();
            }

            if (__instance.def.IsWall)
            {
                var manager = __instance.Map.GetComponent<WallSnowManager>();
                manager.UpdateWallSnowState(__instance);
            }
        }
    }
}
