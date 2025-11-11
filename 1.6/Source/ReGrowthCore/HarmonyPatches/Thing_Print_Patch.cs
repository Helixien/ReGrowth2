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
        public static bool printingSnow = false;

        public static void Postfix(Thing __instance, SectionLayer layer)
        {
            if (ReGrowthUtils.SnowOnWallsPatchWorker.snowOnWalls && __instance.def.IsWall)
            {
                var manager = __instance.Map.GetComponent<WallSnowManager>();
                if (manager.snowCoveredWalls.Contains(__instance))
                {
                    var graphic = manager.GetSnowOverlayGraphic();
                    printingSnow = true;
                    try
                    {
                        graphic.Print(layer, __instance, 0f);
                    }
                    finally
                    {
                        printingSnow = false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Graphic_Linked), "ShouldLinkWith")]
    public static class Graphic_Linked_ShouldLinkWith_Patch
    {
        public static void Postfix(ref bool __result, IntVec3 c, Thing parent)
        {
            if (Thing_Print_Patch.printingSnow && __result)
            {
                var edifice = c.GetEdifice(parent.Map);
                if (edifice is null || edifice.def.IsWall is false)
                {
                    __result = false;
                }
            }
        }
    }
}
