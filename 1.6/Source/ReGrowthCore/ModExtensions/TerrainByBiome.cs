using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static Verse.TerrainDef;

namespace ReGrowthCore
{
    public class TerrainByBiome
    {
        private static readonly int AlphaAddTex = Shader.PropertyToID("_AlphaAddTex");
        private static readonly int BurnTex = Shader.PropertyToID("_BurnTex");
        private static readonly int BurnColor = Shader.PropertyToID("_BurnColor");
        private static readonly int ScrollSpeed = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int BurnScale = Shader.PropertyToID("_BurnScale");
        private static readonly int PollutionTintColor = Shader.PropertyToID("_PollutionTintColor");

        public string label;
        public List<BiomeDef> biomes;

        [NoTranslate]
        public string texturePath;

        [NoTranslate]
        public string pollutedTexturePath;

        [NoTranslate]
        public string pollutionOverlayTexturePath;

        public ShaderTypeDef pollutionShaderType;

        public Color pollutionColor = Color.white;

        public Vector2 pollutionOverlayScrollSpeed = Vector2.zero;

        public Vector2 pollutionOverlayScale = Vector2.one;

        public Color pollutionCloudColor = Color.white;

        public Color pollutionTintColor = Color.white;

        public ThingDef generatedFilth;

        public float? fertility;

        public Graphic graphic = BaseContent.BadGraphic;

        public Graphic graphicPolluted = BaseContent.BadGraphic;

        public Material DrawMatSingle(TerrainDef def)
        {
            if (graphic != null && graphic != BaseContent.BadGraphic)
            {
                return graphic.MatSingle;
            }
            return def.DrawMatSingle;
        }

        public Material DrawMatPolluted(TerrainDef def)
        {
            if (graphicPolluted == BaseContent.BadGraphic)
            {
                if (def.graphicPolluted != BaseContent.BadGraphic)
                {
                    return def.DrawMatPolluted;
                }
                return graphic.MatSingle;
            }
            return graphicPolluted.MatSingle;
        }

        private Shader ShaderPolluted(TerrainDef def)
        {
            if (pollutionShaderType != null)
            {
                return pollutionShaderType.Shader;
            }
            else if (def.pollutionShaderType != null)
            {
                return def.pollutionShaderType.Shader;
            }
            Shader result = null;
            switch (def.edgeType)
            {
                case TerrainEdgeType.Hard:
                    result = ShaderDatabase.TerrainHardPolluted;
                    break;
                case TerrainEdgeType.Fade:
                    result = ShaderDatabase.TerrainFadePolluted;
                    break;
                case TerrainEdgeType.FadeRough:
                    result = ShaderDatabase.TerrainFadeRoughPolluted;
                    break;
            }
            return result;
        }

        public void ResolveData(TerrainDef def)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Shader shader = def.Shader;
                if (texturePath.NullOrEmpty() is false)
                {
                    graphic = GraphicDatabase.Get<Graphic_Terrain>(texturePath, shader, Vector2.one, def.DrawColor, 2000 + def.renderPrecedence);
                    if (shader == ShaderDatabase.TerrainFadeRough || shader == ShaderDatabase.TerrainWater)
                    {
                        graphic.MatSingle.SetTexture(AlphaAddTex, TexGame.AlphaAddTex);
                    }
                }

                if (ModsConfig.BiotechActive &&
                    (!pollutionOverlayTexturePath.NullOrEmpty() || !pollutedTexturePath.NullOrEmpty()))
                {
                    Texture2D texture2D = null;
                    if (!pollutionOverlayTexturePath.NullOrEmpty())
                    {
                        texture2D = ContentFinder<Texture2D>.Get(pollutionOverlayTexturePath);
                    }
                    var shaderPolluted = ShaderPolluted(def);
                    graphicPolluted = GraphicDatabase.Get<Graphic_Terrain>(pollutedTexturePath ?? texturePath,
                        shaderPolluted, Vector2.one, def.DrawColor, 2000 + def.renderPrecedence);
                    Material matSingle = graphicPolluted.MatSingle;
                    if (texture2D != null)
                    {
                        matSingle.SetTexture(BurnTex, texture2D);
                    }
                    matSingle.SetColor(BurnColor, pollutionColor);
                    matSingle.SetVector(ScrollSpeed, pollutionOverlayScrollSpeed);
                    matSingle.SetVector(BurnScale, pollutionOverlayScale);
                    matSingle.SetColor(PollutionTintColor, pollutionTintColor);
                    if (ShaderPolluted(def) == ShaderDatabase.TerrainFadeRoughPolluted)
                    {
                        matSingle.SetTexture(AlphaAddTex, TexGame.AlphaAddTex);
                    }
                }
            });
        }
    }
}

