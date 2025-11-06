using HarmonyLib;
using System;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WeatherWorker), MethodType.Constructor, new Type[] { typeof(WeatherDef) })]
    public static class WeatherWorker_Constructor_Patch
    {
        public static void Postfix(WeatherWorker __instance, WeatherDef def)
        {
            if (__instance.overlays != null)
            {
                for (int i = __instance.overlays.Count - 1; i >= 0; i--)
                {
                    SkyOverlay overlay = __instance.overlays[i];
                    if (overlay is WeatherOverlay_Custom)
                    {
                        __instance.overlays[i] = new WeatherOverlay_Custom
                        {
                            weatherDef = def
                        };
                    }
                }
            }
        }
    }
}
