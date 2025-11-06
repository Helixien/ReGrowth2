using System.Linq;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [StaticConstructorOnStartup]
    public static class RockTextureExtension
    {
        static RockTextureExtension()
        {
            foreach (ThingDef rockDef in DefDatabase<ThingDef>.AllDefs.Where(def => def.building?.isNaturalRock == true && !def.building.isResourceRock))
            {
                StoneExtension textureExtension = rockDef.GetModExtension<StoneExtension>();
                if (textureExtension != null)
                {
                    TerrainDef roughTerrain = rockDef.building.naturalTerrain;
                    TerrainDef hewnTerrain = rockDef.building.leaveTerrain;
                    TerrainDef smoothTerrain = roughTerrain?.smoothedTerrain;

                    if (!string.IsNullOrEmpty(textureExtension.roughTexturePath) && roughTerrain != null
                     && roughTerrain.defName.StartsWith(rockDef.defName + "_Rough"))
                    {
                        roughTerrain.texturePath = textureExtension.roughTexturePath;
                        AddGraphic(roughTerrain);
                    }
                    if (!string.IsNullOrEmpty(textureExtension.hewnTexturePath) && hewnTerrain != null
                    && hewnTerrain.defName.StartsWith(rockDef.defName + "_RoughHewn"))
                    {
                        hewnTerrain.texturePath = textureExtension.hewnTexturePath;
                        AddGraphic(hewnTerrain);
                    }
                    if (!string.IsNullOrEmpty(textureExtension.smoothTexturePath) && smoothTerrain != null
                    && smoothTerrain.defName.StartsWith(rockDef.defName + "_Smooth"))
                    {
                        smoothTerrain.texturePath = textureExtension.smoothTexturePath;
                        AddGraphic(smoothTerrain);
                    }
                }
            }
        }

        private static void AddGraphic(TerrainDef terrain)
        {
            Shader shader = terrain.Shader;
            terrain.graphic = GraphicDatabase.Get<Graphic_Terrain>(terrain.texturePath, shader, Vector2.one, terrain.DrawColor, 2000 + terrain.renderPrecedence);
            if (shader == ShaderDatabase.TerrainFadeRough || shader == ShaderDatabase.TerrainWater)
            {
                terrain.graphic.MatSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
            }
        }
    }
}
