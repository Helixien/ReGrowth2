using System.Reflection;
using HarmonyLib;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch]
    public static class GameCondition_RagingWind_WeatherCommonalityFactor_Patch
    {
        public static bool Prepare() => ModLister.AnyModActiveNoSuffix(["VanillaExpanded.VExplorationE"]);

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("VanillaExplorationExpanded.GameCondition_RagingWind"), "WeatherCommonalityFactor");
        }

        public static void Postfix(ref float __result, WeatherDef weather)
        {
            if (weather == RG_DefOf.RG_FogStorm || weather == RG_DefOf.RG_FoggySnow) __result = 0f;
        }
    }
}
