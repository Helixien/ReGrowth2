using System.Reflection;
using HarmonyLib;
using Verse;
using static ReGrowthCore.ZoneData;

namespace ReGrowthCore
{
    [HarmonyPatch]
    static class Patch_FlowersHarvestHasJobOnCell
    {
        public static bool Prepare() => ModLister.AnyModActiveNoSuffix(["VanillaExpanded.VPEFlowers"]);

        public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("VanillaPlantsExpandedFlowers.WorkGiver_GrowerFlowers_Harvest"), "HasJobOnCell");

        private static bool Prefix(Pawn pawn, IntVec3 c)
        {
            if (ReGrowthCore_SmartFarming.ModSettings.enabled is false || ReGrowthCore_SmartFarming.ModSettings.allowHarvestOption is false) return true;
            var map = pawn.Map;
            var zoneID = map.zoneManager.ZoneAt(c)?.ID ?? -1;
            if (zoneID == -1) return true;
            if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out var mapComp) && mapComp.growZoneRegistry.TryGetValue(zoneID, out var zoneData))
            {
                return zoneData.allowHarvest;
            }
            return true;
        }
    }
}
