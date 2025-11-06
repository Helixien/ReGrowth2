using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch(typeof(Thing), nameof(Thing.Print))]
    public static class Thing_Print_Patch
    {
        [ThreadStatic]
        public static bool snowAltitudeTweak = false;

        public static void Postfix(Thing __instance, SectionLayer layer)
        {
            if (ReGrowthUtils.SnowOnWallsPatchWorker.snowOnWalls && __instance.def.building != null && __instance.def.building.isWall)
            {
                var manager = __instance.Map.GetComponent<WallSnowManager>();
                if (manager.snowCoveredWalls.Contains(__instance))
                {
                    var graphic = manager.GetSnowOverlayGraphic();
                    snowAltitudeTweak = true;
                    try
                    {
                        graphic.Print(layer, __instance, 0f);
                    }
                    finally
                    {
                        snowAltitudeTweak = false;
                    }
                }
            }
        }
    }
}
