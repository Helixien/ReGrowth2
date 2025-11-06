using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
	public class BiomeWorker_BlackSwamp : BiomeWorker
	{
		public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
		{
			if (tile.WaterCovered)
			{
				return -100f;
			}
			if (tile.temperature < -10f)
			{
				return 0f;
			}
			if (tile.rainfall < 600f)
			{
				return 0f;
			}
			if (tile.swampiness < 0.5f)
			{
				return 0f;
			}
			Vector3 tileCenter = Find.WorldGrid.GetTileCenter(planetTile.tileId);
			var value = BiomePerlin.GetNoiseFor(biome).GetValue(tileCenter);
			if (value >= 0.6)
			{
				return (15f + (tile.temperature - 7f) + (tile.rainfall - 600f) / 180f + tile.swampiness * 3f) + 1;
			}
			return -100f;
		}
	}
}
