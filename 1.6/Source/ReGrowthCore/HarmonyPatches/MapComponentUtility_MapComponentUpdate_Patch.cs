using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(MapComponentUtility), "MapComponentUpdate")]
    public static class MapComponentUtility_MapComponentUpdate_Patch
    {
        public static void Postfix(Map map)
        {
            foreach (var terrain in ReGrowthUtils.terrrainWithdefExtensions)
            {
                terrain.Item2.DoWork(terrain.Item1);
            }
        }
    }
}
