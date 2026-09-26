using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore;

[HarmonyPatch(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick))]
public static class SteadyEnvironmentEffects_SteadyEnvironmentEffectsTick_Patch
{
    private static void Postfix(Map ___map)
    {
        var settings = ReGrowthCore_Puddles.ModSettings;
        if (settings == null || !settings.rainWaterPuddles)
        {
            return;
        }

        if (___map.IsHashIntervalTick(60) && ___map.weatherManager.curWeatherAge >= 2500 && ___map.weatherManager.curWeather.snowRate <= 0f && ___map.weatherManager.curWeather.rainRate > 0.1f && ___map.roofGrid != null)
        {
            var area = ___map.Area;
            var currentPuddles = ___map.listerThings.ThingsOfDef(RG_DefOf.RG_FilthWater).Count;

            if (currentPuddles < area / 20)
            {
                var count = Find.TickManager.CurTimeSpeed switch
                {
                    // Probably no need to include paused, but we may as well be safe here
                    TimeSpeed.Normal or TimeSpeed.Paused => settings.normalSpeedPuddles,
                    TimeSpeed.Fast => settings.fastSpeedPuddles,
                    _ => settings.ultrafastSpeedPuddles,
                };

                for (var i = 0; i < count; i++)
                {
                    var cell = ___map.cellsInRandomOrder.Get(Rand.Range(0, ___map.Area));
                    if (!___map.roofGrid.Roofed(cell))
                    {
                        var terrain = cell.GetTerrain(___map);
                        if (terrain.IsWater is false)
                        {
                            FilthMaker.TryMakeFilth(cell, ___map, RG_DefOf.RG_FilthWater);
                        }
                    }
                }
            }
        }
    }
}