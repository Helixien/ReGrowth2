using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    [HotSwappable]
    [StaticConstructorOnStartup]
    public static class ReGrowthUtils
    {
        private static ReGrowthCore_SnowOnWalls _snowOnWallsPatchWorker;
        public static ReGrowthCore_SnowOnWalls SnowOnWallsPatchWorker => _snowOnWallsPatchWorker ??= ReGrowthMod.modPack.Patches.OfType<ReGrowthCore_SnowOnWalls>().FirstOrDefault();
        private static ReGrowthCore_MakeCamp _setUpCampPatchWorker;
        public static ReGrowthCore_MakeCamp MakeCampPatchWorker => _setUpCampPatchWorker ??= ReGrowthMod.modPack.Patches.OfType<ReGrowthCore_MakeCamp>().FirstOrDefault();
        public static List<(TerrainDef, DefExtension_ShaderSpeedMult)> terrrainWithdefExtensions = new List<(TerrainDef, DefExtension_ShaderSpeedMult)>();
        static ReGrowthUtils()
        {
            foreach (var def in DefDatabase<TerrainDef>.AllDefs)
            {
                var extension = def.GetModExtension<TerrainExtension>();
                if (extension != null)
                {
                    if (extension.biomeSpecificTerrains != null)
                    {
                        foreach (var biomeTerrain in extension.biomeSpecificTerrains)
                        {
                            biomeTerrain.ResolveData(def);
                        }
                    }
                }
                var shaderSpeedExtension = def.GetModExtension<DefExtension_ShaderSpeedMult>();
                if (shaderSpeedExtension != null)
                {
                    terrrainWithdefExtensions.Add((def, shaderSpeedExtension));
                }
            }
        }

        public static bool IsHotSpring(this TerrainDef terrainDef)
        {
            return terrainDef == RG_DefOf.HotSpring;
        }

        public static bool IsBathingNow(this Pawn pawn)
        {
            if (pawn?.jobs?.curDriver is null) return false;
            return pawn.jobs.curDriver is JobDriver_Bathe driver_Bathe
            && driver_Bathe.IsBathingNow() || pawn.jobs.curJob.swimming;
        }

        private static Dictionary<Material, Material> materials = new();
        public static Material GetBatheMat(Material baseMat, float transparency)
        {
            if (baseMat is null) return baseMat;
            if (!materials.TryGetValue(baseMat, out var value))
            {
                materials[baseMat] = value = new Material(baseMat);
            }
            value.color = new Color(value.color.r, value.color.g, value.color.b, transparency);
            return value;
        }

        public static bool TryGetBiomeSpecificTerrain(this TerrainDef def, Map map, out TerrainByBiome terrain)
        {
            terrain = null;
            var extension = def?.GetModExtension<TerrainExtension>();
            if (extension?.biomeSpecificTerrains != null)
            {
                var biome = map.Biome;
                foreach (var biomeTerrain in extension.biomeSpecificTerrains)
                {
                    if (biomeTerrain.biomes.Contains(biome))
                    {
                        terrain = biomeTerrain;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
