using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using static ReGrowthCore.ZoneData;

namespace ReGrowthCore
{
    [HarmonyPatch]
    static class Patch_FlowersSowJobOnCell
    {
        private static int lastBlightCheckTick = -1;
        private static readonly Dictionary<int, bool> zoneBlightCache = [];

        public static bool Prepare() => ModLister.AnyModActiveNoSuffix(["VanillaExpanded.VPEFlowers"]);

        public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("VanillaPlantsExpandedFlowers.WorkGiver_GrowerFlowers_Sow"), "JobOnCell");

        private static bool Prefix(Pawn pawn, IntVec3 c)
        {
            if (ReGrowthCore_SmartFarming.ModSettings.enabled is false) return true;
            var map = pawn.Map;
            var zone = map.zoneManager.ZoneAt(c);
            if (zone is not IPlantToGrowSettable || !ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out var comp) || !comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData))
            {
                return true;
            }
            switch (zoneData.sowMode)
            {
                case SowMode.Smart:
                    {
                        if (zoneData.alwaysSow || zoneData.minHarvestDayForNewlySown > -1) break;
                        return false;
                    }
                case SowMode.Force:
                    break;
                case SowMode.On:
                    break;
                case SowMode.Off:
                    return false;
            }
            if (ReGrowthCore_SmartFarming.ModSettings.autoCutBlighted && HasBlightedPlants(zone, map)) return false;
            return true;
        }

        private static bool HasBlightedPlants(Zone zone, Map map)
        {
            if (lastBlightCheckTick != Find.TickManager.TicksGame)
            {
                zoneBlightCache.Clear();
                lastBlightCheckTick = Find.TickManager.TicksGame;
            }
            if (!zoneBlightCache.TryGetValue(zone.ID, out var hasBlight))
            {
                hasBlight = false;
                if (zone.cells.Any(cell => cell.GetPlant(map) is Plant plant && plant.Blighted))
                {
                    hasBlight = true;
                }
                zoneBlightCache[zone.ID] = hasBlight;
            }
            return hasBlight;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var growthSeasonNow = AccessTools.Method(typeof(PlantUtility), nameof(PlantUtility.GrowthSeasonNow), new Type[] { typeof(IntVec3), typeof(Map), typeof(ThingDef) });
            var forceAware = AccessTools.Method(typeof(Patch_FlowersSowJobOnCell), nameof(GrowthSeasonNowForceAware));
            foreach (var code in instructions)
            {
                if ((code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt) && code.OperandIs(growthSeasonNow)) code.operand = forceAware;
                yield return code;
            }
        }

        private static bool GrowthSeasonNowForceAware(IntVec3 c, Map map, ThingDef plantDef)
        {
            if (ReGrowthCore_SmartFarming.ModSettings.enabled)
            {
                var zone = map.zoneManager.ZoneAt(c);
                if (zone is IPlantToGrowSettable && ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out var comp) && comp.growZoneRegistry.TryGetValue(zone.ID, out var zoneData) && zoneData.sowMode == SowMode.Force)
                {
                    return true;
                }
            }
            return PlantUtility.GrowthSeasonNow(c, map, plantDef);
        }
    }
}
