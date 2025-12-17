using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using Verse.AI;
using System.Reflection;
using System.Linq;
using static ReGrowthCore.ZoneData;

namespace ReGrowthCore
{
	//This handles the zone gizmos
	[HarmonyPatch]
	static class Patch_GetGizmos
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			return typeof(Zone).AllSubclasses()
				.Where(t => typeof(IPlantToGrowSettable).IsAssignableFrom(t))
				.Select(t => AccessTools.DeclaredMethod(t, "GetGizmos"))
				.Where(m => m != null);
		}
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Zone __instance)
		{
			if (!ReGrowthCore_SmartFarming.ModSettings.enabled)
			{
				foreach (var value in values)
				{
					yield return value;
				}
				yield break;
			}
			//Pass along all other gizmos except vanilla sow, which we only identify via its hotkey...
			foreach (var value in values)
			{
				if (((Command)value)?.hotKey == KeyBindingDefOf.Command_ItemForbid) continue;
				yield return value;
			}

			Map map = __instance.Map;
			if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming comp) && comp.growZoneRegistry.TryGetValue(__instance.ID, out ZoneData zoneData))
			{
				//Return the sow mode gizmo and priority gizmo
				if (Find.Selector.selected.Count == 1)
				{
					yield return zoneData.sowGizmo;
					yield return zoneData.priorityGizmo;
				}
				else
				{
					foreach (var gizmo in GetMultiZoneGizmos(comp, zoneData, __instance))
					{
						yield return gizmo;
					}
				}

				//Petty jobs gizmo
				yield return zoneData.pettyJobsGizmo;
				//Allow harvest gizmo
				if (AllowHarvest.allowHarvestGizmoPatched) yield return zoneData.allowHarvestGizmo;

				//Harvest now gizmo
				ThingDef crop = (__instance as IPlantToGrowSettable).GetPlantDefToGrow();
				if (crop == null) yield break;

				if (HarvestNowGizmo(__instance, map, crop)) yield return zoneData.harvestGizmo;

				//Orchard align?
				if (ReGrowthCore_SmartFarming.ModSettings.orchardAlignment && crop.plant.blockAdjacentSow) yield return zoneData.orchardGizmo;
			}
		}

		static bool HarvestNowGizmo(Zone zone, Map map, ThingDef plantDefToGrow)
		{
			var cells = zone.cells;
			var thingGrid = map.thingGrid;
			for (int i = cells.Count; i-- > 0;)
			{
				var cell = cells[i];
				if (thingGrid.ThingAt(cell, ThingCategory.Plant) is Plant plant && plant.def == plantDefToGrow && plant.HarvestableNow)
				{
					return true;
				}
			}
			return false;
		}

		static IEnumerable<Gizmo> GetMultiZoneGizmos(MapComponent_SmartFarming comp, ZoneData zoneData, Zone thisZone)
		{
			var firstSelectedGrowZone = Find.Selector.SelectedObjects.OfType<Zone>().FirstOrDefault(z => z is IPlantToGrowSettable);
			if (thisZone != firstSelectedGrowZone)
			{
				yield break;
			}

			var basisZoneData = zoneData;

			yield return new Command_Action()
			{
				defaultLabel = ("SmartFarming.Icon.SetAll".Translate() + basisZoneData.sowGizmo.defaultLabel.ToLower()),
				defaultDesc = basisZoneData.sowGizmo.defaultDesc,
				hotKey = KeyBindingDefOf.Command_ItemForbid,
				icon = basisZoneData.iconCache[basisZoneData.sowMode],
				action = () =>
				{
					var newMode = basisZoneData.sowMode;
					switch (newMode)
					{
						case SowMode.Force: newMode = SowMode.Off; break;
						case SowMode.On: newMode = SowMode.Smart; break;
						case SowMode.Smart: newMode = SowMode.Force; break;
						default: newMode = SowMode.On; break;
					}

					var selectedZones = Find.Selector.SelectedObjects;
					foreach (var obj in selectedZones)
					{
						if (obj is Zone growZone && growZone is IPlantToGrowSettable && comp.growZoneRegistry.TryGetValue(growZone.ID, out ZoneData data))
						{
							data.SwitchSowMode(comp, growZone, newMode);
						}
					}
				}
			};
			var priorityGizmo = new Command_Action()
			{
				defaultLabel = ("SmartFarming.Icon.SetAll".Translate() + basisZoneData.priorityGizmo.defaultLabel.ToLower()),
				defaultDesc = basisZoneData.priorityGizmo.defaultDesc,
				icon = ResourceBank.iconPriority,
				action = () =>
				{
					var newPriority = basisZoneData.priority;
					newPriority = newPriority != ZoneData.Priority.Critical ? ++newPriority : ZoneData.Priority.Low;

					var selectedZones = Find.Selector.SelectedObjects;
					foreach (var obj in selectedZones)
					{
						if (obj is Zone growZone && growZone is IPlantToGrowSettable && comp.growZoneRegistry.TryGetValue(growZone.ID, out ZoneData data))
						{
							data.SwitchPriority(newPriority);
						}
					}
				}
			};

			switch (basisZoneData.priority)
			{
				case ZoneData.Priority.Low:
					priorityGizmo.SetColorOverride(ResourceBank.grey);
					break;
				case ZoneData.Priority.Preferred:
					priorityGizmo.SetColorOverride(ResourceBank.green);
					break;
				case ZoneData.Priority.Important:
					priorityGizmo.SetColorOverride(ResourceBank.yellow);
					break;
				case ZoneData.Priority.Critical:
					priorityGizmo.SetColorOverride(ResourceBank.red);
					break;
				default:
					priorityGizmo.SetColorOverride(Color.white);
					break;
			}
			yield return priorityGizmo;

			if (Find.Selector.selected.Count > 1)
			{
				yield return new Command_Action()
				{
					defaultLabel = "SmartFarming.Icon.MergeZones".Translate(),
					defaultDesc = "SmartFarming.Icon.MergeZones.Desc".Translate(),
					icon = ResourceBank.mergeZones,
					action = () =>
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("SmartFarming.Icon.ConfirmMergeZones".Translate(), () =>
						{
							var selectedGrowZones = Find.Selector.SelectedObjects.OfType<Zone>()
								.Where(z => z is IPlantToGrowSettable)
								.ToList();

							zoneData.MergeZones(thisZone, selectedGrowZones);
						}));
					}
				};
			}
		}
	}

	//This controls whether or not pawns will skip sow jobs based on the seasonable allowance
	[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell))]
	[HarmonyPriority(HarmonyLib.Priority.Last)]
	static class Patch_JobOnCell
	{
	private static int lastBlightCheckTick = -1;
	private static readonly Dictionary<int, bool> zoneBlightCache = new Dictionary<int, bool>();

		static bool Prefix(Pawn pawn, IntVec3 c)
		{
			if (lastBlightCheckTick != Find.TickManager.TicksGame)
			{
				zoneBlightCache.Clear();
				lastBlightCheckTick = Find.TickManager.TicksGame;
			}

			var map = pawn.Map;
			var zone = map.zoneManager.zoneGrid[c.z * map.info.sizeInt.x + c.x];
			if (zone != null && zone is IPlantToGrowSettable && ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming comp) && comp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
			{
				if (ReGrowthCore_SmartFarming.ModSettings.autoCutBlighted)
				{
					bool hasBlight;
					if (!zoneBlightCache.TryGetValue(zone.ID, out hasBlight))
					{
						hasBlight = false;
						foreach (var cell in zone.cells)
						{
							var plant = cell.GetPlant(map);
							if (plant != null && plant.Blighted)
							{
								hasBlight = true;
								break;
							}
						}
						zoneBlightCache[zone.ID] = hasBlight;
					}

					if (hasBlight)
					{
						return false;
					}
				}

				switch (zoneData.sowMode)
				{
					case SowMode.Smart:
						{
							return zoneData.alwaysSow ? true : zoneData.minHarvestDayForNewlySown > -1;
						}
					case SowMode.Force:
						{
							return true;
						}
					case SowMode.On:
						{
							return true; //Vanilla handling
						}
					case SowMode.Off:
						{
							return false;
						}
				}
			}
			return true;
		}
		
		static Job Postfix(Job __result, WorkGiver_GrowerSow __instance, Pawn pawn, IntVec3 c, bool forced = false)
		{
			if (__result == null)
			{
				var map = pawn.Map;
				var zone = map.zoneManager.zoneGrid[c.z * map.info.sizeInt.x + c.x];
				if (zone != null && zone is IPlantToGrowSettable && ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming comp) && comp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
				{
					if (zoneData.sowMode == SowMode.Force)
					{
						if (c.GetVacuum(pawn.Map) >= 0.5f)
						{
							return null;
						}
						if (WorkGiver_Grower.wantedPlantDef == null)
						{
							WorkGiver_Grower.wantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
							if (WorkGiver_Grower.wantedPlantDef == null)
							{
								return null;
							}
						}
						List<Thing> thingList = c.GetThingList(map);
						Zone_Growing zone_Growing = c.GetZone(map) as Zone_Growing;
						bool flag = false;
						for (int i = 0; i < thingList.Count; i++)
						{
							Thing thing = thingList[i];
							if (thing.def == WorkGiver_Grower.wantedPlantDef)
							{
								return null;
							}
							if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
							{
								flag = true;
							}
						}
						if (flag)
						{
							Thing edifice = c.GetEdifice(map);
							if (edifice == null || edifice.def.fertility < 0f)
							{
								return null;
							}
						}
						if (WorkGiver_Grower.wantedPlantDef.plant.diesToLight)
						{
							if (!c.Roofed(map) && !map.GameConditionManager.IsAlwaysDarkOutside)
							{
								JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseUnroofedTrans);
								return null;
							}
							if (map.glowGrid.GroundGlowAt(c, ignoreCavePlants: true) > 0f)
							{
								JobFailReason.Is(WorkGiver_GrowerSow.CantSowCavePlantBecauseOfLightTrans);
								return null;
							}
						}
						if (WorkGiver_Grower.wantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
						{
							return null;
						}
						Plant plant = c.GetPlant(map);
						if (plant != null && plant.def.plant.blockAdjacentSow)
						{
							if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
							{
								return null;
							}
							if (zone_Growing != null && !zone_Growing.allowCut)
							{
								return null;
							}
							if (!forced && plant.TryGetComp<CompPlantPreventCutting>(out var comp2) && comp2.PreventCutting)
							{
								return null;
							}
							if (!PlantUtility.PawnWillingToCutPlant_Job(plant, pawn))
							{
								return null;
							}
							return JobMaker.MakeJob(JobDefOf.CutPlant, plant);
						}
						Thing thing2 = PlantUtility.AdjacentSowBlocker(WorkGiver_Grower.wantedPlantDef, c, map);
						if (thing2 != null)
						{
							if (thing2 is Plant plant2 && pawn.CanReserveAndReach(plant2, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
							{
								IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
								if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
								{
									Zone_Growing zone_Growing2 = c.GetZone(map) as Zone_Growing;
									Zone_Growing zone_Growing3 = plant2.Position.GetZone(map) as Zone_Growing;
									if ((zone_Growing2 != null && !zone_Growing2.allowCut) || (zone_Growing3 != null && !zone_Growing3.allowCut && plant2.def == zone_Growing3.GetPlantDefToGrow()))
									{
										return null;
									}
									if (!forced && thing2.TryGetComp(out CompPlantPreventCutting comp3) && comp3.PreventCutting)
									{
										return null;
									}
									if (PlantUtility.TreeMarkedForExtraction(plant2))
									{
										return null;
									}
									if (!PlantUtility.PawnWillingToCutPlant_Job(plant2, pawn))
									{
										return null;
									}
									return JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
								}
							}
							return null;
						}
						if (WorkGiver_Grower.wantedPlantDef.plant.sowMinSkill > 0 && ((pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < WorkGiver_Grower.wantedPlantDef.plant.sowMinSkill) || (pawn.IsColonyMech && pawn.RaceProps.mechFixedSkillLevel < WorkGiver_Grower.wantedPlantDef.plant.sowMinSkill)))
						{
							JobFailReason.Is("UnderAllowedSkill".Translate(WorkGiver_Grower.wantedPlantDef.plant.sowMinSkill), __instance.def.label);
							return null;
						}
						for (int j = 0; j < thingList.Count; j++)
						{
							Thing thing3 = thingList[j];
							if (!thing3.def.BlocksPlanting())
							{
								continue;
							}
							if (!pawn.CanReserve(thing3, 1, -1, null, forced))
							{
								return null;
							}
							if (thing3.def.category == ThingCategory.Plant)
							{
								if (thing3.IsForbidden(pawn))
								{
									return null;
								}
								if (zone_Growing != null && !zone_Growing.allowCut)
								{
									return null;
								}
								if (!forced && plant.TryGetComp<CompPlantPreventCutting>(out var comp4) && comp4.PreventCutting)
								{
									return null;
								}
								if (!PlantUtility.PawnWillingToCutPlant_Job(thing3, pawn))
								{
									return null;
								}
								if (PlantUtility.TreeMarkedForExtraction(thing3))
								{
									return null;
								}
								return JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
							}
							if (thing3.def.EverHaulable)
							{
								return HaulAIUtility.HaulAsideJobFor(pawn, thing3);
							}
							return null;
						}
						if (!WorkGiver_Grower.wantedPlantDef.CanNowPlantAt(c, map) || !pawn.CanReserve(c, 1, -1, null, forced))
						{
							return null;
						}
						Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
						job.plantDefToSow = WorkGiver_Grower.wantedPlantDef;
						return job;
					}
				}
			}
			return __result;
	}
	}

	//This adds information to the inspector window
	[HarmonyPatch]
	static class Patch_GetInspectString
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			return typeof(Zone).AllSubclasses()
				.Where(t => typeof(IPlantToGrowSettable).IsAssignableFrom(t))
				.Select(t => AccessTools.DeclaredMethod(t, "GetInspectString"))
				.Where(m => m != null);
		}
		static float totalHungerRate = 0f;
		static int lastRecalculateTick = 0;
		static string Postfix(string __result, Zone __instance)
		{
			Map map = __instance.Map;
			if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming mapComp) && mapComp.growZoneRegistry.TryGetValue(__instance.ID, out ZoneData zoneData))
			{
				// Update the hunger cache only when it's being viewed.
				// Since this code is tied to FPS, rather that tick speed, we can't use "Find.TickManager.TicksGame % 480 == 0",
				// as this code may be called on ticks 479 and 481, but not 480. Likewise, we may just pause on tick 480, causing
				// constant recalculations. As an alternative, we keep track of when last we recalculated the hunger rate, and
				// do it again if 480 ticks have passed. And as a precaution against reloading the game, we do it if last recalculation
				// tick happened in the future, instead of the past.
				if (totalHungerRate == 0f || Find.TickManager.TicksGame >= lastRecalculateTick + 480 || Find.TickManager.TicksGame < lastRecalculateTick)
				{
					try
					{
						totalHungerRate = mapComp.CalculateTotalHungerRate();
					}
					catch (Exception ex)
					{
						Log.Warning("[Smart Farming] Error calculating hunger rate" + ex);
						totalHungerRate = 1f;
					}

					lastRecalculateTick = Find.TickManager.TicksGame;
				}

				StringBuilder builder = new StringBuilder(__result, 10);
				if (zoneData.averageGrowth < (__instance as IPlantToGrowSettable).GetPlantDefToGrow()?.plant.harvestMinGrowth)
				{
					if (zoneData.minHarvestDay > 0)
					{
						builder.Append(ResourceBank.minHarvestDay);
						builder.Append(GenDate.DateFullStringAt(zoneData.minHarvestDay, Find.WorldGrid.LongLatOf(map.Tile)));
					}
					else
						builder.Append(ResourceBank.minHarvestDayFail);
				}
				if (zoneData.fertilityAverage != 0)
					builder.Append("SmartFarming.Inspector.Fertility".Translate(zoneData.fertilityAverage.ToStringPercent(), zoneData.fertilityLow.ToStringPercent()));
				if (zoneData.nutritionYield != 0)
				{
					builder.Append(ResourceBank.yield);
					builder.Append(Math.Round(zoneData.nutritionYield, 2));
				}
				if ((__instance as IPlantToGrowSettable).GetPlantDefToGrow()?.plant.harvestedThingDef?.ingestible?.HumanEdible ?? false)
					builder.Append("SmartFarming.Inspector.DaysWorth".Translate(Math.Round(zoneData.nutritionYield * ReGrowthCore_SmartFarming.ModSettings.processedFoodFactor / totalHungerRate, 2)));

				return builder.ToString();
			}
			else return __result;
		}
	}

	//This is for the "auto cut blighted" mod option
	[HarmonyPatch(typeof(Plant), nameof(Plant.CropBlighted))]
	static class AutoCutIfBlighted
	{
		static void Postfix(Plant __instance)
		{
			Map map = __instance.Map;
			if (ReGrowthCore_SmartFarming.ModSettings.autoCutBlighted && map.designationManager.DesignationOn(__instance, DesignationDefOf.CutPlant) == null)
			{
				map.designationManager.AddDesignation(new Designation(__instance, DesignationDefOf.CutPlant));
			}
		}
	}

	//This is for the "auto cut if dying" mod option
	[HarmonyPatch(typeof(Plant), nameof(Plant.MakeLeafless))]
	static class AutoCutIfDying
	{
		static bool Prepare()
		{
			return ReGrowthCore_SmartFarming.ModSettings.autoCutDying;
		}

		static void Prefix(Plant __instance)
		{
			Map map = __instance.Map;
			if (__instance.def.plant.dieIfLeafless && ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming mapComp) &&
				map.zoneManager.ZoneAt(__instance.Position) is Zone zone && zone is IPlantToGrowSettable)
			{
				mapComp.HarvestNow(zone);
			}
		}
	}

	//This is for the "allow harvest" gizmo
	[HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.HasJobOnCell))]
	static class AllowHarvest
	{
		public static bool allowHarvestGizmoPatched = false;
		static bool Prepare()
		{
			allowHarvestGizmoPatched = ReGrowthCore_SmartFarming.ModSettings.allowHarvestOption;
			return ReGrowthCore_SmartFarming.ModSettings.allowHarvestOption;
		}
		static bool Prefix(Pawn pawn, IntVec3 c)
		{
			Map map = pawn?.Map;

			//We don't check the zone type because it's faster for the collection lookup to return with nothing than it is to cast the zone
			int zoneID = map?.zoneManager.zoneGrid[c.z * map.info.sizeInt.x + c.x]?.ID ?? -1;
			if (zoneID == -1) return true;

			if (ReGrowthCore_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming mapComp) && mapComp.growZoneRegistry.TryGetValue(zoneID, out ZoneData zoneData))
			{
				return zoneData.allowHarvest;
			}

			return true;
		}
	}

	//Skip the contigious check for merged zones
	[HarmonyPatch(typeof(Zone), nameof(Zone.CheckContiguous))]
	static class Patch_CheckContiguous
	{
		static bool Prefix(Zone __instance)
		{
			return !(__instance is IPlantToGrowSettable &&
			ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.zoneManager.map.uniqueID, out MapComponent_SmartFarming mapComp) &&
			mapComp.growZoneRegistry.TryGetValue(__instance.ID, out ZoneData zoneData) && zoneData.isMerged);
		}
	}
}
