using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(TerrainGrid), "GetMaterial")]
    public static class TerrainGrid_GetMaterial_Patch
    {
        public static bool Prefix(TerrainGrid __instance, TerrainDef def, bool polluted, ColorDef color, ref Material __result)
        {
            if (def.TryGetBiomeSpecificTerrain(__instance.map, out var biomeTerrain))
            {
                __result = GetMaterial(biomeTerrain, def, polluted, color);
                return false;
            }
            return true;
        }

        private static Dictionary<(TerrainByBiome, bool, ColorDef), Material> terrainMatCache = new Dictionary<(TerrainByBiome, bool, ColorDef), Material>();

        public static Material GetMaterial(TerrainByBiome terrain, TerrainDef def, bool polluted, ColorDef color)
        {
            (TerrainByBiome, bool, ColorDef) key = (terrain, polluted, color);
            if (!terrainMatCache.ContainsKey(key))
            {
                Graphic graphic = (polluted ? terrain.graphicPolluted : terrain.graphic);
                if (graphic is null || graphic == BaseContent.BadGraphic)
                {
                    graphic = (polluted ? def.graphicPolluted : def.graphic);
                }
                if (color != null)
                {
                    terrainMatCache[key] = graphic.GetColoredVersion(def.graphic.Shader,
                        color.color, Color.white).MatSingle;
                }
                else
                {
                    terrainMatCache[key] = (polluted ? terrain.DrawMatPolluted(def) : terrain.DrawMatSingle(def));
                }
            }
            return terrainMatCache[key];
        }
    }
}

