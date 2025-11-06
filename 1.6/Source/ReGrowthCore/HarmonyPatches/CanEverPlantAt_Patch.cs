using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PlantUtility), "CanEverPlantAt",
        new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(Thing), typeof(bool), typeof(bool), typeof(bool) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal }
        )]
    public static class CanEverPlantAt_Patch
    {
        public static void Postfix(ref AcceptanceReport __result, ThingDef plantDef, IntVec3 c, Map map, ref Thing blockingThing, bool canWipePlantsExceptTree = false, bool writeNoReason = false)
        {
            var extension = plantDef?.GetModExtension<PlantExtension>();
            if (extension != null)
            {
                if (extension.terrainListToGrowOnly != null)
                {
                    var terrain = c.GetTerrain(map);
                    if (!extension.terrainListToGrowOnly.Contains(terrain))
                    {
                        if (!writeNoReason)
                        {
                            __result = "RG.TerrainNotSupported".Translate(terrain.LabelCap, plantDef.label);
                        }
                        else
                        {
                            __result = false;
                        }
                    }
                }
            }
        }
    }
}
