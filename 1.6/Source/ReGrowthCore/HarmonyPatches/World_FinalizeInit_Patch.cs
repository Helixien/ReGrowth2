using HarmonyLib;
using RimWorld.Planet;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public static class World_FinalizeInit_Patch
    {
        public static void Prefix()
        {
            WeatherManager_CurWeatherPerceived_Patch.weathers.Clear();
            WeatherOverlay_FogMotes.unroofedCells.Clear();
            SplashesUtility.hardGridSets.Clear();
        }
    }
}
