using Verse;
using RimWorld;
using ModSettingsFramework;
using System;
using System.Linq;
using System.Xml;
using System.Collections.Generic;

namespace ReGrowthCore
{
	[HotSwappable]
	public class ReGrowthCore_SmartFarming : PatchOperationWorker
	{
		private static ReGrowthCore_SmartFarming _handle;
		public static ReGrowthCore_SmartFarming ModSettings => _handle ??= LoadedModManager.GetMod<ReGrowthMod>().Content
			.Patches.OfType<ReGrowthCore_SmartFarming>().FirstOrDefault();
		public static HashSet<ushort> agriWorkTypes = new HashSet<ushort>();
		public static Dictionary<int, MapComponent_SmartFarming> compCache = new Dictionary<int, MapComponent_SmartFarming>();
		public bool enabled = true;
		public bool useAverageFertility;
		public bool autoCutBlighted = true;
		public bool autoCutDying = true;
		public bool logging;
		public bool coldSowing = true;
		public bool autoHarvestNow = true;
		public bool allowHarvestOption;
		public bool orchardAlignment = true;

		public float processedFoodFactor = 1.8f;
		public float pettyJobs = 0.2f;
		public float minTempAllowed = -3f;

		static ReGrowthCore_SmartFarming()
		{
			agriWorkTypes = DefDatabase<WorkGiverDef>.AllDefsListForReading
				.Where(x => x.workType == WorkTypeDefOf.Growing)
				.Select(y => y.index)
				.ToHashSet();
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "enabled", true);
			Scribe_Values.Look(ref useAverageFertility, "useAverageFertility");
			Scribe_Values.Look(ref autoCutBlighted, "autoCutBlighted", true);
			Scribe_Values.Look(ref autoCutDying, "autoCutDying", true);
			Scribe_Values.Look(ref coldSowing, "coldSowing", true);
			Scribe_Values.Look(ref autoHarvestNow, "autoHarvestNow", true);
			Scribe_Values.Look(ref processedFoodFactor, "processedFoodFactor", 1.8f);
			Scribe_Values.Look(ref minTempAllowed, "minTempAllowed", -3f);
			Scribe_Values.Look(ref pettyJobs, "pettyJobs", 0.2f);
			Scribe_Values.Look(ref allowHarvestOption, "allowHarvestOption");
			Scribe_Values.Look(ref orchardAlignment, "orchardAlignment", true);
		}

		public override void CopyFrom(PatchOperationWorker other)
		{
			if (other is ReGrowthCore_SmartFarming o)
			{
				enabled = o.enabled;
				useAverageFertility = o.useAverageFertility;
				autoCutBlighted = o.autoCutBlighted;
				autoCutDying = o.autoCutDying;
				coldSowing = o.coldSowing;
				autoHarvestNow = o.autoHarvestNow;
				processedFoodFactor = o.processedFoodFactor;
				minTempAllowed = o.minTempAllowed;
				pettyJobs = o.pettyJobs;
				allowHarvestOption = o.allowHarvestOption;
				orchardAlignment = o.orchardAlignment;
				logging = o.logging;
			}
		}

		public override void DoSettings(ModSettingsContainer container, Listing_Standard options)
		{
			string buffer = processedFoodFactor.ToString();

			DoCheckbox(options, "SmartFarming.Settings.Enabled".Translate(), ref enabled, "SmartFarming.Settings.Enabled.Desc".Translate());
			if (enabled)
			{
				options.GapLine();
				scrollHeight += 12;
				DoCheckbox(options, "SmartFarming.Settings.AutoHarvestNow".Translate(), ref autoHarvestNow, "SmartFarming.Settings.AutoHarvestNow.Desc".Translate());
				DoCheckbox(options, "SmartFarming.Settings.AutoCutBlighted".Translate(), ref autoCutBlighted, "SmartFarming.Settings.AutoCutBlighted.Desc".Translate());
				DoCheckbox(options, "SmartFarming.Settings.AutoCutDying".Translate(), ref autoCutDying, "SmartFarming.Settings.AutoCutDying.Desc".Translate());
				DoCheckbox(options, "SmartFarming.Settings.ColdSowing".Translate(), ref coldSowing, "SmartFarming.Settings.ColdSowing.Desc".Translate());
				DoCheckbox(options, "SmartFarming.Settings.AllowHarvest".Translate(), ref allowHarvestOption, "SmartFarming.Settings.AllowHarvest.Desc".Translate());
				DoCheckbox(options, "SmartFarming.Settings.OrchardAlignment".Translate(), ref orchardAlignment, "SmartFarming.Settings.OrchardAlignment.Desc".Translate());
				DoSlider(options, "SmartFarming.Settings.PettyJobsSlider".Translate("20%", "1%", "100%", pettyJobs.ToStringPercent()), ref pettyJobs, pettyJobs.ToStringPercent(), 0.01f, 1f, "SmartFarming.Settings.PettyJobs".Translate(), 10f);
				DoLabel(options, "SmartFarming.Settings.SmartSowLabel".Translate(), null);
				DoCheckbox(options, "SmartFarming.Settings.UseAverageFertility".Translate(), ref useAverageFertility, "SmartFarming.Settings.UseAverageFertility.Desc".Translate());
				DoSlider(options, "SmartFarming.Settings.MinTempSlider".Translate("-3C", "-10C", "5C", Math.Round(minTempAllowed, 1)), ref minTempAllowed, minTempAllowed.ToString(), -10f, 5f, "SmartFarming.Settings.MinTemp".Translate(), 10f);
				DoLabel(options, "SmartFarming.Settings.ProcessedFoodLabel".Translate(), null);

				options.TextFieldNumeric(ref processedFoodFactor, ref buffer, 0f, 99f);
				scrollHeight += 29;

				DoLabel(options, "SmartFarming.Settings.ProcessedFood.Desc".Translate(), null);

				if (Prefs.DevMode)
					DoCheckbox(options, "DevMode: Enable logging", ref logging, null);
			}
		}

		public override void Reset()
		{
			enabled = true;
			useAverageFertility = false;
			autoCutBlighted = true;
			autoCutDying = true;
			coldSowing = true;
			autoHarvestNow = true;
			allowHarvestOption = false;
			orchardAlignment = true;
			processedFoodFactor = 1.8f;
			pettyJobs = 0.2f;
			minTempAllowed = -3f;
			logging = false;
		}

		public override void ApplySettings()
		{
			base.ApplySettings();
			if (Current.ProgramState == ProgramState.Playing)
			{
				try
				{
					foreach (var map in Find.Maps)
					{
						map.GetComponent<MapComponent_SmartFarming>()?.ProcessZones();
					}
				}
				catch (Exception ex)
				{
					Log.Error("[Smart Farming] Error registering new grow zone:\n" + ex);
				}
			}
		}
	}
}
