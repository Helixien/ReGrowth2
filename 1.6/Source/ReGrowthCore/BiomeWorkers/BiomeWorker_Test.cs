using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class BiomeWorker_Test : BiomeWorker
    {
        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            Vector3 tileCenter = Find.WorldGrid.GetTileCenter(planetTile.tileId);
            var value = BiomePerlin.GetNoiseFor(BiomeDef.Named("TemperateForest")).GetValue(tileCenter);
            if (value >= 0.2)
            {
                return 50;
            }
            return -999f;
        }
    }
}
