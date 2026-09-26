using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace ReGrowthCore
{
	public class HardSurface : DefModExtension { }
	public class HardStuff : DefModExtension { }

	[StaticConstructorOnStartup]
	public static class SplashesUtility
	{
		public static HashSet<ushort> hardTerrains;
		public static Dictionary<int, Vector3[]> hardGrids = new Dictionary<int, Vector3[]>();
		public static Vector3[] activeMapHardGrid;
		public static FastRandom fastRandom = new FastRandom();
		public static FleckSystem fleckSystemCache;
		public static int splashRate = 40, arrayChunks = 0, chunkIndex = 0, adjustedSplashRate = 40, activeMapID = -1;
		const int chunkSize = 1000;

		//Happens once on game start, goes through the database to find defs it thinks is hard and rain would bounce off of
		static SplashesUtility()
		{
			List<ushort> workingList = new List<ushort>();
			List<string> report = new List<string>();

			HashSet<StuffCategoryDef> hardStuff = new HashSet<StuffCategoryDef>();
			var list = DefDatabase<StuffCategoryDef>.defsList;
			for (int i = list.Count; i-- > 0;)
			{
				var thingDef = list[i];
				if (thingDef.HasModExtension<HardStuff>()) hardStuff.Add(thingDef);
			}

			//Go through every terrain in the game
			var list2 = DefDatabase<TerrainDef>.defsList;
			for (int i = list2.Count; i-- > 0;)
			{
				var terrainDef = list2[i];
				bool flag = false;
				//Does it have a cost?
				if (terrainDef.costList != null)
				{
					//Look through the costs
					for (int j = terrainDef.costList.Count; j-- > 0;)
					{
						var thingDefCountClass = terrainDef.costList[j];
						//See if any of them are metallatic or stony, which we define as hard surfaces that rain would splash off of
						if (thingDefCountClass.thingDef?.stuffProps?.categories?.Any(x => hardStuff.Contains(x)) ?? false)
						{
							flag = true;
							break;
						}
					}
				}
				else if (terrainDef.HasModExtension<HardSurface>() || terrainDef.defName.Contains("_Rough")) flag = true;
				if (flag)
				{
					workingList.Add(terrainDef.index);
					report.Add(terrainDef.label);
				}
			}
			hardTerrains = new HashSet<ushort>(workingList);
			if (Prefs.DevMode) Log.Message("[Simple FX: Splashes] The following terrains have been defined as being hard:\n - " + string.Join("\n - ", report));
		}

		private static readonly FastRandom deterministicRand = new FastRandom();

		private static int GetCellSeed(Map map, IntVec3 c)
		{
			return map.uniqueID ^ c.x ^ (c.z << 16);
		}

		private static Vector3 GetDeterministicOffset(Map map, IntVec3 c)
		{
			deterministicRand.Reinitialise(GetCellSeed(map, c));
			Vector3 vector = c.ToVector3Fast();
			return new Vector3(vector.x + ((deterministicRand.Next(100) - 50) / 100f), vector.y, vector.z + ((deterministicRand.Next(100) - 50) / 100f));
		}

		private static bool PassesNatureFilter(Map map, IntVec3 c)
		{
			deterministicRand.Reinitialise(GetCellSeed(map, c) + 1);
			return deterministicRand.Next(100) < (ReGrowthCore_SimpleFX.ModSettings.natureFilter * 100);
		}

		public static void ProcessSplashes(Map map)
		{
			if (deterministicRand.NextBool() && deterministicRand.NextBool() && activeMapHardGrid != null) //This looks dumb, but it's gating more complex code behind 2 ultra-fast random bool checks.
			{
				if (fleckSystemCache == null) Find.CurrentMap.flecks.systems.TryGetValue(RG_DefOf.RG_Splash.fleckSystemClass, out fleckSystemCache);

				//Chunk start
				int chunkStart = (int)(chunkIndex * chunkSize);
				//Chunk end
				int chunkEnd = System.Math.Min(activeMapHardGrid.Length, (int)((chunkIndex * chunkSize) + chunkSize));

				for (int i = chunkStart; i < chunkEnd; ++i)
				{
					if (deterministicRand.Next(adjustedSplashRate) == 0)
					{
						var splashAt = activeMapHardGrid[i];
						if (!CameraDriver.lastViewRect.Contains(splashAt.ToIntVec3())) continue;
						fleckSystemCache.CreateFleck(FleckMaker.GetDataStatic(splashAt, map, RG_DefOf.RG_Splash, ReGrowthCore_SimpleFX.ModSettings.sizeMultiplier));
					}
				}
				if (++chunkIndex == arrayChunks) chunkIndex = 0;
			}
		}

		public static void RebuildCache(Map map)
		{
			//First, ensure the key is set
			hardGrids.AddDistinct(map.uniqueID, null);

			//Generate a working list
			List<Vector3> workingList = new List<Vector3>();
			for (int i = map.info.NumCells; i-- > 0;)
			{
				//Fetch the def cell by cell
				TerrainDef terrainDef = map.terrainGrid.topGrid[i];
				var cell = map.cellIndices.IndexToCell(i);
				//The cell must be a valid def, not roofed, and not fogged
				if (hardTerrains.Contains(terrainDef.index) &&
					(!terrainDef.natural || PassesNatureFilter(map, cell)) &&
					map.roofGrid.roofGrid[i] == null &&
					!map.fogGrid.IsFogged(i)) workingList.Add(GetDeterministicOffset(map, cell));
			}

			//Record
			hardGrids[map.uniqueID] = workingList.ToArray();

			SetActiveGrid(map);
		}

		public static void UpdateCache(Map map, IntVec3 c, TerrainDef def = null)
		{
			if (map == null) return;
			Vector3 vector = GetDeterministicOffset(map, c);
			if (hardGrids.TryGetValue(map.uniqueID, out Vector3[] hardGrid))
			{
				if (hardGrid.NullOrEmpty())
				{
					hardGrid = new Vector3[0];
				}

				//Add the new cell if relevant
				if (def == null) def = map.terrainGrid.TerrainAt(map.cellIndices.CellToIndex(c));
				bool isHard = hardTerrains.Contains(def.index) &&
				              (!def.natural || PassesNatureFilter(map, c)) &&
				              !c.Roofed(map) &&
				              !map.fogGrid.IsFogged(c);
				var list = new List<Vector3>(hardGrid);
				bool contains = list.Contains(vector);

				//Filter out this cell
				if (isHard)
				{
					if (!contains)
					{
						list.Add(vector);
						hardGrids[map.uniqueID] = list.ToArray();
						SetActiveGrid(map);
					}
				}
				else
				{
					if (contains)
					{
						list.Remove(vector);
						hardGrids[map.uniqueID] = list.ToArray();
						SetActiveGrid(map);
					}
				}
			}
		}

		public static void SetActiveGrid(Map map)
		{
			//Update the active grid.
			if (map != null && Find.CurrentMap?.uniqueID == map.uniqueID && hardGrids.TryGetValue(map.uniqueID, out activeMapHardGrid))
			{
				if (activeMapHardGrid.Length == 0)
				{
					arrayChunks = 0;
					adjustedSplashRate = 1;
					activeMapID = -1;
					return;
				}
				arrayChunks = (int)System.Math.Ceiling(activeMapHardGrid.Length / (float)chunkSize);
				chunkIndex = 0;
				//Adjusted splash rate
				adjustedSplashRate = (int)System.Math.Ceiling((splashRate * ReGrowthCore_SimpleFX.ModSettings.splashRarity) / arrayChunks);
				activeMapID = map.uniqueID;
			}
		}

		public static void ResetCache()
		{
			hardGrids.Clear();
			activeMapHardGrid = null;
			fleckSystemCache = null;
		}
	}
}
