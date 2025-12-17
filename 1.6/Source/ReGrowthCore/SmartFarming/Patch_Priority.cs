using System;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ReGrowthCore
{
	//This tells the workgiver that growing is a priority-enabled job
	[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.Prioritized), MethodType.Getter)]
	public static class Patch_WorkGiver_Scanner_Prioritized
	{
		public static bool Postfix(bool __result, WorkGiver_Scanner __instance)
		{
			return __result || (ReGrowthCore_SmartFarming.ModSettings.enabled && ReGrowthCore_SmartFarming.agriWorkTypes.Contains(__instance.def.index));
		}
	}

	//This returns the priority value (higher = more urgent)
	[HarmonyPatch(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.GetPriority), new Type[] { typeof(Pawn), typeof(TargetInfo) })]
	public static class Patch_WorkGiver_Scanner_GetPriority
	{
		public static float Postfix(float __result, Pawn pawn, TargetInfo t, WorkGiver_Scanner __instance)
		{
			if (!ReGrowthCore_SmartFarming.agriWorkTypes.Contains(__instance.def.index)) return __result;

			Map map = pawn.Map;
			var zone = map.zoneManager.zoneGrid[t.cellInt.z * map.info.sizeInt.x + t.cellInt.x];

			if (zone == null)
			{
				return 2f; //This would be a hydroponic
			}

			if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming mapComp) && mapComp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
			{
				return (float)zoneData.priority;
			}
			return __result;
		}
	}

	//This color-codes the selection edge borders to priority
	[HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
	public static class Patch_DrawSelectionBracketFor
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			bool ran = false;
			var method = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawFieldEdges),
			new Type[] { typeof(List<IntVec3>), typeof(int) });
			foreach (var code in instructions)
			{
				if (!ran && code.Calls(method))
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(MapComponent_SmartFarming).GetMethod(nameof(MapComponent_SmartFarming.DrawFieldEdges)));
					ran = true;
				}
				else
				{
					yield return code;
				}
			}
			if (!ran) Log.Warning("[Smart Farming] Transpiler could not find target for field edge drawer. There may be a mod conflict, or RimWorld updated?");
		}
	}
}
