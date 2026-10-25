using HarmonyLib;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
    public static class Game_DeinitAndRemoveMap_Patch
    {
        public static void Postfix(Map map)
        {
            WeatherManager_CurWeatherPerceived_Patch.weathers.Remove(map);
            WeatherOverlay_FogMotes.unroofedCells.Remove(map);
        }
    }
}
