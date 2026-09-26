using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(GenStep_ScatterLumpsMineable), nameof(GenStep_ScatterLumpsMineable.ScatterAt))]
    public static class GenStep_ScatterLumpsMineable_ScatterAt_Patch
    {
        public static void Postfix(GenStep_ScatterLumpsMineable __instance, Map map)
        {
            if (SpaceRockManager.isGeneratingOresForAsteroidStep)
            {
                foreach (IntVec3 cellInLump in __instance.recentLumpCells)
                {
                    List<Thing> thingsInCell = map.thingGrid.ThingsListAt(cellInLump);
                    if (thingsInCell == null) continue;

                    for (int i = 0; i < thingsInCell.Count; i++)
                    {
                        Thing thingInCell = thingsInCell[i];
                        if (thingInCell is Mineable mineable)
                        {
                            mineable.MarkAsSpaceRock();
                        }
                    }
                }
            }
            else
            {
                Log.Message("Not generating ores for asteroid step");
            }
        }
    }
}
