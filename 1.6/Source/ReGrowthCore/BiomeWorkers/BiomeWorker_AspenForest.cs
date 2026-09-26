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
			if (tile.temperature is < -5f or > 0f)
			{
				return 0f;
			}
			if (tile.rainfall < 300f)
			{
				return 0f;
			}
			var tileCenter = Find.WorldGrid.GetTileCenter(planetTile.tileId);
			float value = BiomePerlin.GetNoiseFor(biome).GetValue(tileCenter);
			if (value >= 0.05f)
			{
				return -tile.temperature + value;
			}
			return 0f;
		}
	}
}
