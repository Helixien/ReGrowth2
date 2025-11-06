using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WeatherDecider), "CurrentWeatherCommonality")]
    public static class WeatherDecider_CurrentWeatherCommonality_Patch
    {
        private static ReGrowthCore_WeatherStates _handle;
        public static ReGrowthCore_WeatherStates Handle => _handle ??= LoadedModManager.GetMod<ReGrowthMod>().Content
            .Patches.OfType<ReGrowthCore_WeatherStates>().FirstOrDefault();
        public static void Postfix(ref float __result, WeatherDef weather)
        {
            if (Handle != null)
            {
                if (Handle.weatherDefStates.TryGetValue(weather.defName, out var state))
                {
                    if (state is false)
                    {
                        __result = 0f;
                    }
                }
            }

        }
    }
}
