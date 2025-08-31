using HarmonyLib;
using RimWorld.Planet;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;
using ReGrowthCore;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WorldDrawLayer_Hills), nameof(WorldDrawLayer_Hills.Regenerate))]
    public static class Patch_WorldDrawLayer_Hills_Regenerate
    {
        private static readonly IntVec2 TexturesInAtlas = new IntVec2(2, 2);

        // Explicit overload binding for GetSubMesh(Material)
        private static readonly MethodInfo getSubMeshMethod =
            typeof(WorldDrawLayerBase).GetMethod(
                "GetSubMesh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Material) },
                null
            );

        // Explicit overload binding for FinalizeMesh(MeshParts)
        private static readonly MethodInfo finalizeMeshMethod =
            typeof(WorldDrawLayerBase).GetMethod(
                "FinalizeMesh",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(MeshParts) },
                null
            );

        private static LayerSubMesh GetSubMesh(WorldDrawLayer instance, Material material)
        {
            return (LayerSubMesh)getSubMeshMethod.Invoke(instance, new object[] { material });
        }

        private static void FinalizeMesh(WorldDrawLayer instance, MeshParts parts)
        {
            finalizeMeshMethod.Invoke(instance, new object[] { parts });
        }

        public static bool Prefix(WorldDrawLayer_Hills __instance, ref IEnumerable __result)
        {
            if (!ReGrowthMod.WorldBeautificationIsActive || !ReGrowthMod.worldBeautificationToggle)
            {
                return true;
            }

            // Force clear by accessing the subMeshes field directly
            var subMeshesField = typeof(WorldDrawLayerBase).GetField("subMeshes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (subMeshesField != null)
            {
                var subMeshes = subMeshesField.GetValue(__instance);
                var clearMethod = subMeshes.GetType().GetMethod("Clear");
                clearMethod?.Invoke(subMeshes, null);
            }

            __result = CustomRegenerate(__instance);
            return false; // skip original completely
        }

        private static IEnumerable CustomRegenerate(WorldDrawLayer_Hills instance)
        {
            //Log.Message("[WMBP] Custom WorldDrawLayer_Hills.Regenerate");

            Rand.PushState();
            Rand.Seed = Find.World.info.Seed;
            WorldGrid worldGrid = Find.WorldGrid;

            for (int i = 0; i < worldGrid.TilesCount; i++)
            {
                SurfaceTile tile = worldGrid[i];
                Vector3 tileCenter = worldGrid.GetTileCenter(i);
                BiomesKitControl modExt = tile.biome.GetModExtension<BiomesKitControl>();
                if (modExt != null)
                {
                    bool noRoads = !tile.PrimaryBiome.allowRoads || tile.potentialRoads == null;
                    bool noRivers = !tile.PrimaryBiome.allowRivers || tile.potentialRivers == null;

                    // --- Unique Hills ---
                    if (modExt.uniqueHills && noRoads && noRivers)
                    {
                        string path = "WorldMaterials/BiomesKit/" + tile.PrimaryBiome.defName + "/Hills/";
                        float scale = 1f;

                        switch (tile.hilliness)
                        {
                            case Hilliness.Flat: path = null; break;
                            case Hilliness.SmallHills:
                                scale = Rand.Range(modExt.smallHillSizeMultiplier - 0.1f, modExt.smallHillSizeMultiplier + 0.1f);
                                path += tile.temperature < modExt.snowpilesBelow ? "SmallSnowpiles" : "SmallHills";
                                break;
                            case Hilliness.LargeHills:
                                scale = Rand.Range(modExt.largeHillSizeMultiplier - 0.1f, modExt.largeHillSizeMultiplier + 0.1f);
                                path += tile.temperature < modExt.snowpilesBelow ? "LargeSnowpiles" : "LargeHills";
                                break;
                            case Hilliness.Mountainous:
                                scale = Rand.Range(modExt.mountainSizeMultiplier - 0.1f, modExt.mountainSizeMultiplier + 0.1f);
                                path += "Mountains" + SnowSuffix(tile.temperature, modExt.mountainsSemiSnowyBelow, modExt.mountainsSnowyBelow, modExt.mountainsVerySnowyBelow, modExt.mountainsFullySnowyBelow);
                                break;
                            case Hilliness.Impassable:
                                scale = Rand.Range(modExt.impassableSizeMultiplier - 0.1f, modExt.impassableSizeMultiplier + 0.1f);
                                path += "Impassable" + SnowSuffix(tile.temperature, modExt.impassableSemiSnowyBelow, modExt.impassableSnowyBelow, modExt.impassableVerySnowyBelow, modExt.impassableFullySnowyBelow);
                                break;
                        }

                        if (path != null)
                        {
                            Material mat = MaterialPool.MatFrom(path, ShaderDatabase.WorldOverlayTransparentLit, 3600);
                            var subMesh = GetSubMesh(instance, mat);
                            Vector3 posForTangents = tileCenter;
                            WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, posForTangents, worldGrid.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false, 0.01f, false);
                            WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                        }
                    }

                    // --- Forested Plains ---
                    if (modExt.forested && tile.hilliness == Hilliness.Flat && noRoads && noRivers)
                    {
                        string path = "WorldMaterials/BiomesKit/" + tile.PrimaryBiome.defName + "/Forest/Forest_";
                        bool modified = false;

                        if (tile.temperature < modExt.forestSnowyBelow)
                        {
                            path += "Snowy";
                            modified = true;
                        }

                        if (tile.rainfall < modExt.forestSparseBelow)
                        {
                            path += "Sparse";
                            modified = true;
                        }
                        else if (tile.rainfall > modExt.forestDenseAbove)
                        {
                            path += "Dense";
                            modified = true;
                        }

                        if (!modified)
                            path = path.TrimEnd('_');

                        Material mat = MaterialPool.MatFrom(path, ShaderDatabase.WorldOverlayTransparentLit, modExt.materialLayer);
                        var subMesh = GetSubMesh(instance, mat);
                        Vector3 posForTangents = tileCenter;
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, posForTangents, worldGrid.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false, 0.01f, false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }

                    // --- Custom Material Overlay ---
                    if (modExt.materialPath != "World/MapGraphics/Default")
                    {
                        Material mat = MaterialPool.MatFrom(modExt.materialPath, ShaderDatabase.WorldOverlayTransparentLit, modExt.materialLayer);
                        var subMesh = GetSubMesh(instance, mat);
                        Vector3 posForTangents = tileCenter;
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, posForTangents, worldGrid.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false, 0.01f, false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }
                }
            }

            Rand.PopState();
            FinalizeMesh(instance, MeshParts.All);

            yield break; // must yield because Regenerate is IEnumerable
        }

        private static string SnowSuffix(float temp, float semi, float snowy, float verySnowy, float fullySnowy)
        {
            if (temp < fullySnowy) return "_FullySnowy";
            if (temp < verySnowy) return "_VerySnowy";
            if (temp < snowy) return "_Snowy";
            if (temp < semi) return "_SemiSnowy";
            return "";
        }
    }
}
