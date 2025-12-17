using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ReGrowthCore
{
	//This tells the workgiver that growing is a priority-enabled job
	[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
	static class Patch_WorkGiver_GrowerSow_JobOnCell
	{
		static bool Prepare()
		{
			return ReGrowthCore_SmartFarming.ModSettings.enabled && ReGrowthCore_SmartFarming.ModSettings.orchardAlignment;
		}

		static Job Postfix(Job __result, IntVec3 c, Pawn pawn)
		{
			//Is relevant? Plants that need clearance only...
			if (__result?.plantDefToSow?.plant?.blockAdjacentSow != true) return __result;

			Map map = pawn.Map;

			//Only applies to zones, and skip 1x1 planter zones
			Zone zone = map.zoneManager.ZoneAt(c);
			if (zone == null || zone.cells.Count == 1) return __result;

			if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming mapComp) &&
				mapComp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData) &&
				zoneData.orchardAlignment)
			{
				var refCell = zoneData.cornerCell;
				if (ReGrowthCore_SmartFarming.ModSettings.logging && Prefs.DevMode) map.debugDrawer.FlashCell(refCell, text: "REF");

				return ((refCell.x & 1) == 0) == ((c.x & 1) == 0) && ((refCell.z & 1) == 0) == ((c.z & 1) == 0) ? __result : null;
			}
			return __result;
		}
	}
}
