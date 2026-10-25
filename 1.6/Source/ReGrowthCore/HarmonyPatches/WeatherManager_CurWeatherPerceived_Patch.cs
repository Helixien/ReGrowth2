using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WeatherManager), "CurWeatherPerceived", MethodType.Getter)]
    public static class WeatherManager_CurWeatherPerceived_Patch
    {
        public static readonly Dictionary<Map, WeatherDef> weathers = [];

        private static void Postfix(WeatherManager __instance, WeatherDef __result)
        {
            if (weathers.TryGetValue(__instance.map, out WeatherDef value) && __result != value)
            {
                weathers[__instance.map] = __result;
                var options = __result.GetModExtension<WeatherLetterExtensions>();
                if (options != null)
                {
                    Find.LetterStack.ReceiveLetter(options.letterTitle, options.letterText, options.letterDef);
                }
            }
            else
            {
                weathers[__instance.map] = __result;
            }
        }
    }
}
