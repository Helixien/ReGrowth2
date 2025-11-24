using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using static ReGrowthCore.SplashesUtility;
using UnityEngine;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ReGrowthCore
{
	//Build the terrain cache
	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	class Patch_Map_FinalizeInit
	{
		static void Postfix(Map __instance)
		{
			RebuildCache(__instance);
		}
	}

	//When the map changes, change the fleck system cache
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.Notify_SwitchedMap))]
	class Patch_Notify_SwitchedMap
	{
		static void Postfix()
		{
			Map map = Find.CurrentMap;
			if (map != null)
			{
				map.flecks.systems.TryGetValue(RG_DefOf.RG_Splash.fleckSystemClass, out fleckSystemCache);
				SetActiveGrid(map);
			}
		}
	}

	//The main process loop is attached to the weather ticker
	[HarmonyPatch(typeof(Map), nameof(Map.MapPreTick))]
	class Patch_Map_MapPreTick
	{
		static void Postfix(Map __instance)
		{
			var curWeather = __instance.weatherManager.curWeather;
			if (activeMapID == __instance.uniqueID &&
			curWeather.rainRate > 0f &&
			curWeather.snowRate == 0f) SplashesUtility.ProcessSplashes(__instance);
		}
	}

	//If there's a terrain change...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.SetTerrain))]
	class Patch_TerrainGrid_SetTerrain { static void Postfix(TerrainGrid __instance, IntVec3 c, TerrainDef newTerr) { UpdateCache(__instance.map, c, newTerr); } }

	//If there's a terrain removal...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.RemoveTopLayer))]
	class Patch_TerrainGrid_RemoveTopLayer { static void Postfix(TerrainGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

	//If there's a roof change...
	[HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
	class Patch_RoofGrid_SetRoof { static void Postfix(RoofGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

	//Update cache if a map is removed
	[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
	class Patch_Game_DeinitAndRemoveMap
	{
		static void Postfix(Map map)
		{
			if (map != null)
			{
				hardGrids.Remove(map.uniqueID);
				SetActiveGrid(Find.CurrentMap);
			}
		}
	}

	//Flush the cache on reloads
	[HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
	class Patch_World_FinalizeInit
	{
		static void Postfix()
		{
			ResetCache();
		}
	}

	[HarmonyPatch(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.DoCellSteadyEffects))]
	static class Patch_DoCellSteadyEffects
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var mapInfo = AccessTools.Field(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.map));
			var rangeInfo = AccessTools.Property(typeof(Room), nameof(Room.Temperature)).GetGetMethod();
			bool ran = false;
			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == rangeInfo)
				{
					codes.InsertRange(i + 2, new List<CodeInstruction>(){

						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldloc_S, 8), //temperature
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, mapInfo),
						new CodeInstruction(OpCodes.Call, typeof(Patch_DoCellSteadyEffects).GetMethod(nameof(Patch_DoCellSteadyEffects.ColdGlow)))
					});
					ran = true;
					break;
				}
			}
			if (!ran) Log.Warning("[Simple FX: Freezers] Transpiler could not find target. There may be a mod conflict, or RimWorld updated?");
			return codes.AsEnumerable();
		}

		static FastRandom fastRandom = new FastRandom();

		static public void ColdGlow(IntVec3 c, float temperature, Map map)
		{
			if (ReGrowthCore_SimpleFX.ModSettings.considerOutdoors)
			{
				try
				{
					if (map.mapTemperature.OutdoorTemp < 0f) return;
				}
				catch
				{
					return;
				}
			}
			if (temperature < 0f && map == Current.gameInt.CurrentMap && CameraDriver.lastViewRect.ExpandedBy(64).Contains(c))
			{
				FleckDef fleckDef = temperature < -8f ? RG_DefOf.RG_VeryColdGlow : RG_DefOf.RG_ColdGlow;

				FleckCreationData dataStatic = FleckMaker.GetDataStatic(
					new Vector3(c.x + fastRandom.Next(1, 50) / 100f, 10.54054f, c.z + fastRandom.Next(1, 50) / 100f), map, fleckDef, fastRandom.Next(200, 300) / 100f);
				dataStatic.rotationRate = fastRandom.Next(-300, 300) / 100f;
				dataStatic.velocityAngle = (float)fastRandom.Next(0, 360);
				dataStatic.velocitySpeed = 0.12f;
				map.flecks.CreateFleck(dataStatic);
			}
		}
	}
}
