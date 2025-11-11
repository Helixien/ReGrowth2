using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(SteadyEnvironmentEffects), "DoCellSteadyEffects", new Type[] { typeof(IntVec3) })]
    public static class SteadyEnvironmentEffects_DoCellSteadyEffects_Patch
    {
        public static void Postfix(IntVec3 c, Map ___map)
        {
            var settings = ReGrowthCore_Puddles.ModSettings;
            if (settings == null || !settings.rainWaterPuddles)
            {
                return;
            }

            if (___map.weatherManager.curWeatherAge >= 2500 && ___map.weatherManager.curWeather.snowRate <= 0f && ___map.weatherManager.curWeather.rainRate > 0.1f && Rand.Value <= settings.puddleChance && ___map.roofGrid != null && !___map.roofGrid.Roofed(c))
            {
                int maxPuddles = (___map.Size.x * ___map.Size.z) / 20;
                int currentPuddles = ___map.listerThings.ThingsOfDef(RG_DefOf.RG_FilthWater).Count;

                if (currentPuddles < maxPuddles)
                {
                    var terrain = c.GetTerrain(___map);
                    if (terrain.IsWater is false)
                    {
                        if (c.GetEdifice(___map) is null)
                        {
                            FilthMaker.TryMakeFilth(c, ___map, RG_DefOf.RG_FilthWater, 1);
                        }
                    }
                }
            }
        }
    }
}
