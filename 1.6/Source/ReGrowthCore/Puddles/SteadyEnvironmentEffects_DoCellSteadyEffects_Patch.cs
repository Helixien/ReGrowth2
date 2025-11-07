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

            if (___map.weatherManager.curWeather.rainRate > 0.1f && Rand.Value <= settings.puddleChance && ___map.roofGrid != null && !___map.roofGrid.Roofed(c))
            {
                var terrain = c.GetTerrain(___map);
                if (terrain.IsWater is false)
                {
                    if (c.GetEdifice(___map) is null)
                    {
                        if (Rand.Chance(0.8f))
                        {
                            FleckMaker.Static(c.ToVector3Shifted(), ___map, RG_DefOf.RG_WaterSpatter, Rand.Range(0.5f, 1.5f));
                        }
                        else
                        {
                            FleckMaker.Static(c.ToVector3Shifted(), ___map, RG_DefOf.RG_Puddle, Rand.Range(0.5f, 1.5f));
                        }
                    }
                }
            }
        }
    }
}
