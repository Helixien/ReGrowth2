using System.Collections;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{

    public class WorldDrawLayer_Beautification : WorldDrawLayer
    {
        private const int BelowLandmarksRenderQueue = 3505;

        private static readonly FloatRange BaseSizeRange = new FloatRange(0.9f, 1.1f);

        private static readonly IntVec2 TexturesInAtlas = new IntVec2(2, 2);

        private static readonly FloatRange BasePosOffsetRange_SmallHills = new FloatRange(0f, 0.37f);

        private static readonly FloatRange BasePosOffsetRange_LargeHills = new FloatRange(0f, 0.2f);

        private static readonly FloatRange BasePosOffsetRange_Mountains = new FloatRange(0f, 0.08f);

        private static readonly FloatRange BasePosOffsetRange_ImpassableMountains = new FloatRange(0f, 0.08f);

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
                    bool hillDrawn = false;

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

                        var hillSizeMultiplier = modExt.materialSizeMultiplier;
                        switch (singleTile.hilliness)
                        {
                            case Hilliness.SmallHills: hillSizeMultiplier *= modExt.smallHillSizeMultiplier; break;
                            case Hilliness.LargeHills: hillSizeMultiplier *= modExt.largeHillSizeMultiplier; break;
                            case Hilliness.Mountainous: hillSizeMultiplier *= modExt.mountainSizeMultiplier; break;
                            case Hilliness.Impassable: hillSizeMultiplier *= modExt.impassableSizeMultiplier; break;
                        }
                        if (path != null && ContentFinder<Texture2D>.Get(path, reportFailure: false) == null)
                        {
                            path = singleTile.hilliness switch
                            {
                                Hilliness.SmallHills => "WorldMaterials/BiomesKit/Default/Hills/SmallHills",
                                Hilliness.LargeHills => "WorldMaterials/BiomesKit/Default/Hills/LargeHills",
                                Hilliness.Mountainous => "WorldMaterials/BiomesKit/Default/Hills/Mountains",
                                Hilliness.Impassable => "WorldMaterials/BiomesKit/Default/Hills/Impassable",
                                _ => null
                            };
                        }
                        if (path != null && ContentFinder<Texture2D>.Get(path, reportFailure: false) != null)
                        {
                            var color = Color.white;
                            var layer = modExt.materialLayer;
                            if (singleTile.Landmark != null)
                            {
                                color = new Color(1f, 1f, 1f, 0.5f);
                                layer = BelowLandmarksRenderQueue;
                            }
                            Material mat = MaterialPool.MatFrom(path, ShaderDatabase.WorldOverlayTransparentLit, color, layer);
                            var subMesh = GetSubMesh(mat);

                            WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                                singleTile.Layer.AverageTileSize * hillSizeMultiplier, 0.005f, subMesh, false,
                                0.01f, false);
                            WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                            hillDrawn = true;
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

                        if (ContentFinder<Texture2D>.Get(path, reportFailure: false) == null) continue;
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
                    if (!hillDrawn)
                    {
                        DrawVanillaHills(singleTile, tileCenter);
                    }
                }
                else if (!ReGrowthMod.DefaultWMBPTextureFallback)
                {
                    DrawVanillaHills(singleTile, tileCenter);
                }
                else
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

                    if (hillPath != null && ContentFinder<Texture2D>.Get(hillPath, reportFailure: false) != null)
                    {
                        var color = Color.white;
                        var layer = 3515;
                        if (singleTile.Landmark != null)
                        {
                            color = new Color(1f, 1f, 1f, 0.5f);
                            layer = BelowLandmarksRenderQueue;
                        }
                        Material mat = MaterialPool.MatFrom(hillPath, ShaderDatabase.WorldOverlayTransparentLit,
                            color, layer);
                        var subMesh = GetSubMesh(mat);
                        WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, tileCenter,
                            singleTile.Layer.AverageTileSize, 0.005f, subMesh, false, Rand.Range(0f, 360f),
                            false);
                        WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, 2), Rand.Range(0, 2), 2, 2, subMesh);
                    }
                    else
                    {
                        DrawVanillaHills(singleTile, tileCenter);
                    }
                }
            }

            Rand.PopState();
            FinalizeMesh(MeshParts.All);

            yield break; // must yield because Regenerate is IEnumerable
        }

        private void DrawVanillaHills(Tile tile, Vector3 tileCenter)
        {
            if (tile.Landmark is not null)
            {
                return;
            }
            Material material;
            FloatRange posOffsetRange;
            switch (tile.hilliness)
            {
                case Hilliness.SmallHills:
                    material = WorldMaterials.SmallHills;
                    posOffsetRange = BasePosOffsetRange_SmallHills;
                    break;
                case Hilliness.LargeHills:
                    material = WorldMaterials.LargeHills;
                    posOffsetRange = BasePosOffsetRange_LargeHills;
                    break;
                case Hilliness.Mountainous:
                    material = WorldMaterials.Mountains;
                    posOffsetRange = BasePosOffsetRange_Mountains;
                    break;
                case Hilliness.Impassable:
                    material = WorldMaterials.ImpassableMountains;
                    posOffsetRange = BasePosOffsetRange_ImpassableMountains;
                    break;
                default:
                    return;
            }
            var subMesh = GetSubMesh(material);
            var posForTangents = tileCenter;
            var magnitude = tileCenter.magnitude;
            tileCenter = (tileCenter + Rand.UnitVector3 * (posOffsetRange.RandomInRange * planetLayer.AverageTileSize)).normalized * magnitude;
            WorldRendererUtility.PrintQuadTangentialToPlanet(tileCenter, posForTangents, BaseSizeRange.RandomInRange * planetLayer.GetTileSize(tile.tile), 0.005f, subMesh, false, Rand.Range(0f, 360f), false);
            WorldRendererUtility.PrintTextureAtlasUVs(Rand.Range(0, TexturesInAtlas.x), Rand.Range(0, TexturesInAtlas.z), TexturesInAtlas.x, TexturesInAtlas.z, subMesh);
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
