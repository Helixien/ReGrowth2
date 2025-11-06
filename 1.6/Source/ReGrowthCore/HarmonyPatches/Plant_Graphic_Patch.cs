using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Plant), "Graphic", MethodType.Getter)]
    public static class Plant_Graphic_Patch
    {
        public static bool Prefix(Plant __instance, ref Graphic __result)
        {
            if (__instance.def.plant?.immatureGraphicPath.NullOrEmpty() == false)
            {
                if (__instance.def.plant.harvestMinGrowth <= 0f)
                {
                    if (__instance.Growth < 0.25f)
                    {
                        __result = __instance.def.plant.immatureGraphic;
                        return false;
                    }
                }
                else
                {
                    if (__instance.Growth < __instance.def.plant.harvestMinGrowth)
                    {
                        __result = __instance.def.plant.immatureGraphic;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
