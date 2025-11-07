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

            if (___map.weatherManager.curWeather.snowRate <= 0f && ___map.roofGrid != null && !___map.roofGrid.Roofed(c))
            {
                if (___map.weatherManager.curWeather.rainRate > 0.1f && Rand.Value <= settings.puddleChance && ___map.roofGrid != null && !___map.roofGrid.Roofed(c))
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

                if ((float)___map.weatherManager.curWeatherAge >= 7500f && (___map.weatherManager.curWeather.rainRate <= 0f))
                {
                    Thing thing2 = c.GetThingList(___map).Where(delegate (Thing t)
                    {
                        return (t.def == RG_DefOf.RG_FilthWater || t.def == RG_DefOf.RG_FilthWaterSpatter);
                    }).FirstOrDefault();

                    if (thing2 != null && Rand.Value <= 0.2f)
                    {
                        ((Filth)thing2).ThinFilth();
                    }
                }
            }
        }
    }
}
