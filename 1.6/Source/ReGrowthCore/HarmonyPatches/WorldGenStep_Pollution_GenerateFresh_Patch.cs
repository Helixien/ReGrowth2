using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WorldGenStep_Pollution), "GenerateFresh")]
    public static class WorldGenStep_Pollution_GenerateFresh_Patch
    {
        public static void Postfix(WorldGenStep_Pollution __instance, string seed)
        {
            WorldGrid worldGrid = Find.WorldGrid;
            Perlin perlin = new Perlin(0.10000000149011612, 2.0, 0.5, 6, seed.GetHashCode(), QualityMode.Medium);
            __instance.tmpTiles.Clear();
            __instance.tmpTileNoise.Clear();
            for (int i = 0; i < worldGrid.TilesCount; i++)
            {
                if (worldGrid[i].biome.GetModExtension<BiomeExtension>()?.pollutionRange != null)
                {
                    __instance.tmpTiles.Add(i);
                    __instance.tmpTileNoise.Add(i, perlin.GetValue(worldGrid.GetTileCenter(i)));
                }
            }
            __instance.tmpTiles.SortByDescending(tile => __instance.tmpTileNoise[tile]);
            foreach (var tile in __instance.tmpTiles)
            {
                var extension = worldGrid[tile].biome.GetModExtension<BiomeExtension>();
                worldGrid[tile].pollution = Mathf.Lerp(extension.pollutionRange.Value.min, extension.pollutionRange.Value.max, __instance.tmpTileNoise[tile]);
            }
            __instance.tmpTiles.Clear();
            __instance.tmpTileNoise.Clear();
        }
    }
}

