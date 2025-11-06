using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ReGrowthCore
{
	public class BiomeWorker_AspenForest : BiomeWorker
	{
		public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
		{
			if (tile.WaterCovered)
			{
				return -100f;
			}
			if (tile.temperature < -5f)
			{
				return 0f;
			}
			if (tile.rainfall < 300f)
			{
				return 0f;
			}
			var tileCenter = Find.WorldGrid.GetTileCenter(planetTile.tileId);
			float value = BiomePerlin.GetNoiseFor(biome).GetValue(tileCenter);
			float tileTemperature = 0f - tile.temperature;
			if (value >= 0.05f && tileTemperature >= 0)
			{
				return tileTemperature + value;
			}
			return 0f;
		}
	}
}
