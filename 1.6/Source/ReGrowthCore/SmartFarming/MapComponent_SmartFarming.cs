using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System;
using static ReGrowthCore.ZoneData;
using RimWorld.Planet;

namespace ReGrowthCore
{
	public class MapComponent_SmartFarming : MapComponent
	{
		int ticks, currentDay, hour, sunrise, sunset, lastMessageDay = -1;
		PlanetTile tile;
		public Dictionary<int, ZoneData> growZoneRegistry = new Dictionary<int, ZoneData>();
		public float tempOffsetCache, latitude, longitudeTuning, baseTemperature, worldAverage, sunLow, sunHigh;
		List<string> report = new List<string>();
		World world;

		public MapComponent_SmartFarming(Map map) : base(map) { }

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref this.growZoneRegistry, "growZoneRegistry");
		}
		public override void FinalizeInit()
		{
			//Quick cache
			try
			{
				ReGrowthCore_SmartFarming.compCache.Add(map.uniqueID, this);
			}
			catch (System.Exception ex)
			{
				Log.Message("[Smart Farming] Tried to register a component on a map that already was registered. Did the cache not flush properly? " + ex);
			}

			tile = map.Parent is PocketMapParent parent ? parent.sourceMap.Tile : map.Tile;
			//Cache some frequently used getters that don't change
			latitude = Find.WorldGrid.LongLatOf(tile).x;
			world = Find.World;
			worldAverage = Season.Winter.GetMiddleTwelfth(0f).GetBeginningYearPct();
			float latitudeAb = System.Math.Abs(latitude);
			int tmp = (int)(30000f * (latitudeAb > 90f ? 90f / latitudeAb : latitudeAb / 90f));
			sunrise = 15000 + tmp;
			sunset = 47500 + tmp;

			//Cache longitude tuning
			if (world.grid.LongLatOf(tile).y >= 0f) longitudeTuning = TemperatureTuning.SeasonalTempVariationCurve.Evaluate(world.grid.DistanceFromEquatorNormalized(tile));
			else longitudeTuning = -TemperatureTuning.SeasonalTempVariationCurve.Evaluate(world.grid.DistanceFromEquatorNormalized(tile));

			//Add placeholder registy if missing
			if (growZoneRegistry == null) growZoneRegistry = new Dictionary<int, ZoneData>();

			//Find any missing zones (for when the mod is installed for an existing save)
			foreach (Zone zone in map.zoneManager.AllZones)
			{
				if (zone is IPlantToGrowSettable && !growZoneRegistry.ContainsKey(zone.ID))
				{
					growZoneRegistry.Add(zone.ID, new ZoneData());
					growZoneRegistry[zone.ID].Init(this, zone);
					CalculateAll(zone);
				}
			}

			//Sanity check
			var allValidZones = map.zoneManager.AllZones.Where(x => x is IPlantToGrowSettable).Select(y => y.ID);
			foreach (var zoneData in growZoneRegistry.ToList())
			{
				int zoneID = zoneData.Key;
				if (!allValidZones.Contains(zoneID))
				{
					if (Prefs.DevMode) Log.Message("[Smart Farming] Removing invalid key # " + zoneID);
					growZoneRegistry.Remove(zoneID);
				}
				else
				{
					zoneData.Value.Init(this, map.zoneManager.AllZones.FirstOrDefault(x => x.ID == zoneID));
				}
			}

			//Ensure the pawn cache is set
			var tmp2 = this.map.mapPawns.FreeColonistsAndPrisoners;
		}

		private void CalculateAverages(Zone zone, ZoneData zoneData)
		{
			int numOfCells = zone.cells.Count, numOfPlants = 0, newPlants = 0;
			float fertility = 0f, lowestFertility = 99f, growth = 0f;

			for (int n = 0; n < numOfCells; ++n)
			{
				//Fertility calculations
				IntVec3 index = zone.cells[n];
				float fertilityHere = map.fertilityGrid.FertilityAt(index);
				fertility += fertilityHere;
				if (fertilityHere != 0f && fertilityHere < lowestFertility) lowestFertility = fertilityHere;

				//Plant tally
				Plant plant = map.thingGrid.ThingAt(index, ThingCategory.Plant) as Plant;
				if (plant != null && plant.def.index == (zone as IPlantToGrowSettable).GetPlantDefToGrow()?.index)
				{
					growth += plant.growthInt;
					++numOfPlants;
					if (plant.growthInt < 0.08f) ++newPlants;
				}
			}

			//Save fertility data
			zoneData.fertilityAverage = fertility / numOfCells;
			zoneData.fertilityLow = lowestFertility;

			//Save growth data
			zoneData.averageGrowth = (numOfPlants > 0) ? growth / numOfPlants : 0f;

			//Process petty jobs since we already have all the data needed
			if (zoneData.noPettyJobs && zoneData.sowMode != SowMode.Off)
			{
				float validPlants = numOfPlants - newPlants;
				var field = zone.GetType().GetField("allowSow");
				if (field != null)
				{
					field.SetValue(zone, !(validPlants > 0 && 1 - (validPlants / (float)numOfCells) < ReGrowthCore_SmartFarming.ModSettings.pettyJobs));
				}
			}
			//If not using petty jobs, validate the sowmode
			else
			{
				var field = zone.GetType().GetField("allowSow");
				if (field != null)
				{
					field.SetValue(zone, zoneData.sowMode != SowMode.Off);
				}
			}
		}

		private long CalculateDaysToHarvest(Zone zone, ZoneData zoneData, bool forSowing = false)
		{
			//Check for toxic fallout first
			if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) && !map.roofGrid.Roofed(zone.Position))
			{
				return -1;
			}

			ThingDef plant = (zone as IPlantToGrowSettable).GetPlantDefToGrow();
			if (plant == null) return -1;

			//Prepare variables
			List<string> simulationReport = new List<string>();
			int growthNeeded = (int)(plant.plant.growDays * plant.plant.harvestMinGrowth * 60000f * 1.1f * (1f - (forSowing ? 0f : zoneData.averageGrowth / plant.plant.harvestMinGrowth)));
			int simulatedGrowth = 0;
			int numOfDays = 0;
			int lifespan = (int)plant.plant.LifespanDays;

			while (simulatedGrowth < growthNeeded && simulatedGrowth != -1)
			{
				simulatedGrowth = SimulateDay(numOfDays, simulatedGrowth, zone, plant, zoneData, simulationReport);

				if (++numOfDays > 360)
				{
					Log.Warning("[Smart Farming] failed simulating " + plant.defName + " at zone " + zone.Position);
					simulatedGrowth = -1;
				}
				else if (lifespan < numOfDays) simulatedGrowth = -1;
			}

			if (ReGrowthCore_SmartFarming.ModSettings.logging && Prefs.DevMode)
			{
				string reportPrint = simulationReport.Count > 0 ? ("\n" + string.Join("\n", simulationReport)) : "skipped";
				report.Add(" - " + (forSowing ? "new sowing " : "") + "report for " +
					zone.Position.ToString() + " (" + (zone as IPlantToGrowSettable).GetPlantDefToGrow()?.defName + ") : " + reportPrint);
			}

			return simulatedGrowth == -1 ? -1 : (numOfDays * 60000) + Find.TickManager.TicksAbs;
		}

		int SimulateDay(int numOfDays, int simulatedGrowth, Zone zone, ThingDef plant, ZoneData zoneData, List<string> simulationReport)
		{
			int ticksOfLight = 32500; // 32500 = 60,000 ticks * .54167, only the hours this plant is "awake"

			//This adjusts the ticks of light if we're doing a partial day calculation, depending on what hour it currently is
			if (numOfDays == 0)
			{
				hour = 14 - hour;
				if (hour < 1)
				{
					if (hour < -3) hour = 13; //Past midnight, consider it a full day
					else hour = 0;
				}

				ticksOfLight = 2500 * hour;
			}

			//Prepare date
			int dayToSimulate = numOfDays + currentDay;

			//Fertility
			float fertilityFactor = PlantUtility.GrowthRateFactorFor_Fertility(plant, ReGrowthCore_SmartFarming.ModSettings.useAverageFertility ? zoneData.fertilityAverage : zoneData.fertilityLow);
			int growthToday = (int)(ticksOfLight * fertilityFactor);

			//Temperature
			int numOfDayTicks = (int)(dayToSimulate * 60000);
			float low = baseTemperature + OffsetFromSeasonCycle((int)(dayToSimulate * 60000) + sunrise) + sunLow + tempOffsetCache;
			float high = baseTemperature + OffsetFromSeasonCycle((int)(dayToSimulate * 60000) + sunset) + sunHigh + tempOffsetCache;
			float average = (low + high) / 2f;
			growthToday = (int)(growthToday * PlantUtility.GrowthRateFactorFor_Temperature(plant, average));

			//Results, use -1 if the plan will die/never grow
			simulatedGrowth = (fertilityFactor == 0 || (plant.plant.dieIfLeafless && Math.Min(low, high) < ReGrowthCore_SmartFarming.ModSettings.minTempAllowed)) ? -1 : simulatedGrowth + growthToday;

			//Special check for freezing ground
			if (!ReGrowthCore_SmartFarming.ModSettings.coldSowing && numOfDays == 0 && low <= 0) simulatedGrowth = -1;

			//Dangerously cold tonight? Mark plants for harvest nd Give a message and avoid message spam
			if (ReGrowthCore_SmartFarming.ModSettings.autoHarvestNow && numOfDays < 2 && simulatedGrowth == -1 && HarvestNow(zone) > 0 && currentDay != lastMessageDay)
			{
				lastMessageDay = currentDay;
				Messages.Message("SmartFarming.Message.AutoHarvestNow".Translate(), MessageTypeDefOf.NeutralEvent, false);
			}

			//Debug
			if (ReGrowthCore_SmartFarming.ModSettings.logging && Prefs.DevMode)
			{
				simulationReport.Add("   - day: " + dayToSimulate +
					" | temperature: " + Math.Round(low, 2) + " to " + Math.Round(high, 2) +
					" | growth: " + simulatedGrowth.ToString() +
					" | fertility: " + fertilityFactor.ToStringPercent() +
					" | temperature: " + PlantUtility.GrowthRateFactorFor_Temperature(plant, average).ToStringPercent());
			}

			return simulatedGrowth;
		}

		float OffsetFromSeasonCycle(int absTick)
		{
			float num = (float)(absTick / 60000 % 60) / 60f;
			return (float)System.Math.Cos(6.2831855f * (num - worldAverage)) * -longitudeTuning;
		}

		void CalculateYield(Zone zone, ZoneData zoneData)
		{
			//Reset
			zoneData.nutritionYield = 0f;
			var plantDefToGrow = (zone as IPlantToGrowSettable).GetPlantDefToGrow();
			if (plantDefToGrow == null) return;

			//Fetch plant's produce
			if (plantDefToGrow.plant.harvestedThingDef == null) return;

			//Calculate the yield
			float num = plantDefToGrow.plant.harvestYield * Current.gameInt.storyteller.difficulty.cropYieldFactor * zone.cells.Count;
			if (zoneData.nutritionCache == 0) zoneData.nutritionCache = plantDefToGrow?.plant?.harvestedThingDef?.GetStatValueAbstract(StatDefOf.Nutrition, null) ?? 0;
			zoneData.nutritionYield = zoneData.nutritionCache * num;
		}

		public float CalculateTotalHungerRate()
		{
			float totalHungerRate = 0; //Reset
			var pawns = map.mapPawns.FreeColonistsAndPrisoners;
			foreach (Pawn pawn in pawns)
			{
				totalHungerRate += (Need_Food.BaseHungerRate(pawn.ageTracker.CurLifeStage, pawn.def) * 60000f) * HungerCategory.Fed.HungerMultiplier() * pawn.health.hediffSet.GetHungerRateFactor(null) *
				(pawn.story?.traits?.HungerRateFactor ?? 1f);
			}
			return totalHungerRate;
		}

		public override void MapComponentTick()
		{
			if (!ReGrowthCore_SmartFarming.ModSettings.enabled)
			{
				return;
			}
			if (++ticks == 2500) //Hourly
			{
				ticks = 0;
				ProcessZones();
			}
		}

		public void ProcessZones()
		{
			UpdateCommonCache();

			report.Clear();
			foreach (Zone zone in map.zoneManager.AllZones)
			{
				if (zone is IPlantToGrowSettable) CalculateAll(zone, false);
			}
			if (ReGrowthCore_SmartFarming.ModSettings.logging && Prefs.DevMode) Log.Message("[Smart Farming] Simulation report: \n" + string.Join("\n", report));
		}

		public void CalculateAll(Zone zone, bool cacheNow = true)
		{
			if (growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
			{
				//Sanity check
				if (map == null || map.gameConditionManager == null)
				{
					Log.Message("[Smart Farming] Tried to process an unknown zone.");
					return;
				}

				if (cacheNow) UpdateCommonCache(); //The only method that calls this method with a false bool is the hourly update

				CalculateAverages(zone, zoneData);
				zoneData.minHarvestDay = CalculateDaysToHarvest(zone, zoneData, false);
				zoneData.minHarvestDayForNewlySown = CalculateDaysToHarvest(zone, zoneData, true);
				CalculateYield(zone, zoneData);
				var plantDefToGrow = (zone as IPlantToGrowSettable).GetPlantDefToGrow();
				//Sanity check on alwaysSow in case settings were changed
				if (ReGrowthCore_SmartFarming.ModSettings.coldSowing && zoneData.sowMode == SowMode.Smart && plantDefToGrow != null && !plantDefToGrow.plant.dieIfLeafless &&
					(plantDefToGrow.plant.forceIsTree || plantDefToGrow.plant.harvestTag == "Wood"))
				{
					zoneData.alwaysSow = true;
				}
			}
		}

		void UpdateCommonCache()
		{
			tempOffsetCache = map.gameConditionManager.AggregateTemperatureOffset();
			currentDay = GenDate.DayOfYear(Current.gameInt.tickManager.TicksAbs, latitude);
			baseTemperature = world.grid[tile].temperature;
			sunLow = (float)System.Math.Cos(6.2831855f * (GenDate.DayPercent((long)(currentDay * 60000) + sunrise, latitude) + 0.32f)) * 7f;
			sunHigh = (float)System.Math.Cos(6.2831855f * (GenDate.DayPercent((long)(currentDay * 60000) + sunset, latitude) + 0.32f)) * 7f;
			hour = GenDate.HourOfDay(Current.gameInt.tickManager.ticksGameInt, latitude);
		}

		public int HarvestNow(Zone zone, bool roofCheck = true, bool checkSensitivity = true)
		{
			if (zone == null) return 0;
			ThingDef crop = (zone as IPlantToGrowSettable).GetPlantDefToGrow();
			if (crop == null) return 0;

			int result = 0;
			var cells = zone.cells;
			var thingGrid = map.thingGrid;
			var designationManager = map.designationManager;
			var roofGrid = map.roofGrid;
			for (int i = cells.Count; i-- > 0;)
			{
				if (thingGrid.ThingAt<Plant>(zone.cells[i]) is Plant plant && plant.def == crop && //Is this the right plant?
				plant.HarvestableNow && //Ready for harvest?
				(!checkSensitivity || plant.def.plant.dieIfLeafless) && //Can die to the cold?
				!designationManager.HasMapDesignationOn(plant) &&  //Is not already designatd?
				(!roofCheck || !roofGrid.Roofed(plant.Position))) //Is not roofed?
				{
					++result;
					designationManager.AddDesignation(new Designation(plant, DesignationDefOf.HarvestPlant));
				}
			}
			return result;
		}

		public static void DrawFieldEdges(List<IntVec3> cells, int renderQueue, Zone zone)
		{
			if (zone is IPlantToGrowSettable &&
				ReGrowthCore_SmartFarming.compCache.TryGetValue(Find.CurrentMap?.uniqueID ?? -1, out MapComponent_SmartFarming mapComp) &&
				mapComp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
			{
				UnityEngine.Color color;
				switch (zoneData.priority)
				{
					case ReGrowthCore.ZoneData.Priority.Low:
						{
							color = ResourceBank.grey; break;
						}
					case ReGrowthCore.ZoneData.Priority.Preferred:
						{
							color = ResourceBank.green; break;
						}
					case ReGrowthCore.ZoneData.Priority.Important:
						{
							color = ResourceBank.yellow; break;
						}
					case ReGrowthCore.ZoneData.Priority.Critical:
						{
							color = ResourceBank.red; break;
						}
					default:
						{
							color = ResourceBank.white; break;
						}
				}

				GenDraw.DrawFieldEdges(cells, color, null, renderQueue: renderQueue);
			}
			GenDraw.DrawFieldEdges(cells, renderQueue: renderQueue);
		}
	}
}
