using System.Reflection;
using HarmonyLib;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch]
    public static class GameCondition_NoWind_WeatherCommonalityFactor_Patch
    {
        public static bool Prepare() => ModLister.HasActiveModWithName("Vanilla Landmarks Expanded");

        public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("VanillaExplorationExpanded.GameCondition_NoWind"), "WeatherCommonalityFactor");

        public static void Postfix(ref float __result, WeatherDef weather)
        {
            if (weather == RG_DefOf.RG_Windy) __result = 0f;
        }
    }
}
