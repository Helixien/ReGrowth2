using System.Collections;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{

    public class WorldDrawLayer_Beautification : WorldDrawLayer
    {
        public override bool Visible
        {
            get
            {
                if (base.Visible)
                {
                    if (ReGrowthMod.WorldBeautificationIsActive &&
                        ReGrowthMod.worldBeautificationToggle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override IEnumerable Regenerate()
        {
            foreach (object item in base.Regenerate())
            {
                yield return item;
            }

            Rand.PushState();
            Rand.Seed = Find.World.info.Seed;

            List<Tile> worldGrid = planetLayer.Tiles;
            for (int i = 0; i < worldGrid.Count; i++)
            {
                Tile singleTile = worldGrid[i];
                SurfaceTile surfaceTile = null;
                // Other planetLayers may not have a SurfaceTile
                if (worldGrid[i] is SurfaceTile)
                    surfaceTile = (SurfaceTile)worldGrid[i];

                Vector3 tileCenter = Find.WorldGrid.GetTileCenter(worldGrid[i].tile);

                if (singleTile.PrimaryBiome?.GetModExtension<BiomesKitControl>() is { } modExt)
                {
                    bool noRoads = !singleTile.PrimaryBiome.allowRoads || surfaceTile?.potentialRoads == null;
                    bool noRivers = !singleTile.PrimaryBiome.allowRivers || surfaceTile?.potentialRivers == null;

                    // --- Unique Hills ---
                    if (modExt.uniqueHills && noRoads && noRivers)
                    {
                        string path = "WorldMaterials/BiomesKit/" + singleTile.PrimaryBiome.defName + "/Hills/";

                        switch (singleTile.hilliness)
                        {
                            case Hilliness.Flat: path = null; break;
                            case Hilliness.SmallHills:
                                path += singleTile.temperature < modExt.snowpilesBelow ? "SmallSnowpiles" : "SmallHills";
                                break;
                            case Hilliness.LargeHills:
                                path += singleTile.temperature < modExt.snowpilesBelow ? "LargeSnowpiles" : "LargeHills";
                                break;
                            case Hilliness.Mountainous:
                                path += "Mountains" + SnowSuffix(singleTile.temperature, modExt.mountainsSemiSnowyBelow,
                                    modExt.mountainsSnowyBelow, modExt.mountainsVerySnowyBelow,
                                    modExt.mountainsFullySnowyBelow);
                                break;
                            case Hilliness.Impassable:
                                path += "Impassable" + SnowSuffix(singleTile.temperature, modExt.impassableSemiSnowyBelow,
                                    modExt.impassableSnowyBelow, modExt.impassableVerySnowyBelow,
                                    modExt.impassableFullySnowyBelow);
                                break;
                        }

                        if (path != null)
                        {
                            Material mat = MaterialPool.MatFrom(path, ShaderDatabase.WorldOverlayTransparentLit, 3600);
                            var subMesh = GetSubMesh(mat);

                            WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                                singleTile.Layer.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false,
                                0.01f, false);
                            WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                        }
                    }

                    // --- Forested Plains ---
                    if (modExt.forested && singleTile.hilliness == Hilliness.Flat && noRoads && noRivers)
                    {
                        string path = "WorldMaterials/BiomesKit/" + singleTile.PrimaryBiome.defName + "/Forest/Forest_";
                        bool modified = false;

                        if (singleTile.temperature < modExt.forestSnowyBelow)
                        {
                            path += "Snowy";
                            modified = true;
                        }

                        if (singleTile.rainfall < modExt.forestSparseBelow)
                        {
                            path += "Sparse";
                            modified = true;
                        }
                        else if (singleTile.rainfall > modExt.forestDenseAbove)
                        {
                            path += "Dense";
                            modified = true;
                        }

                        if (!modified)
                            path = path.TrimEnd('_');

                        Material mat = MaterialPool.MatFrom(path, ShaderDatabase.WorldOverlayTransparentLit,
                            modExt.materialLayer);
                        var subMesh = GetSubMesh(mat);
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                            singleTile.Layer.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false, 0.01f,
                            false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }

                    // --- Custom Material Overlay ---
                    if (modExt.materialPath != "World/MapGraphics/Default")
                    {
                        Material mat = MaterialPool.MatFrom(modExt.materialPath, ShaderDatabase.WorldOverlayTransparentLit,
                            modExt.materialLayer);
                        var subMesh = GetSubMesh(mat);
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                            singleTile.Layer.AverageTileSize * modExt.materialSizeMultiplier, 0.005f, subMesh, false, 0.01f,
                            false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }
                }
                else if (ReGrowthMod.DefaultWMBPTextureFallback)
                {
                    //Fallback for if biome does not have any textures.
                    //Will revert to base game graphics
                    //Setting Disabled by default
                    string hillPath = null;
                    switch (singleTile.hilliness)
                    {
                        case Hilliness.SmallHills:
                            hillPath = "WorldMaterials/BiomesKit/Default/Hills/SmallHills";
                            break;
                        case Hilliness.LargeHills:
                            hillPath = "WorldMaterials/BiomesKit/Default/Hills/LargeHills";
                            break;
                        case Hilliness.Mountainous:
                            hillPath = "WorldMaterials/BiomesKit/Default/Hills/Mountains";
                            break;
                        case Hilliness.Impassable:
                            hillPath = "WorldMaterials/BiomesKit/Default/Hills/Impassable";
                            break;
                    }

                    if (hillPath != null)
                    {
                        Material mat = MaterialPool.MatFrom(hillPath, ShaderDatabase.WorldOverlayTransparentLit,
                            3515);
                        var subMesh = GetSubMesh(mat);
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                            singleTile.Layer.AverageTileSize, 0.005f, subMesh, false, Rand.Range(0f, 360f),
                            false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }
                }
            }

            Rand.PopState();
            FinalizeMesh(MeshParts.All);

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
