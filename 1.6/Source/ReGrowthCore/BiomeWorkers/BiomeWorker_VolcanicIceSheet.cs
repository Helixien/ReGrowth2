using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.Noise;

// Copyright Helixien 2020

namespace ReGrowthCore
{
	public class RGW_VolcanicIceSheet_BiomeWorker : BiomeWorker
	{
		public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
		{
			if (tile.WaterCovered)
			{
				return -100f;
			}

			var perlin = new Perlin(0.079999998211860657, 2.0, 1, 6, Rand.Int, QualityMode.High);
			var tileCenter = Find.WorldGrid.GetTileCenter(planetTile.tileId);
			float value = perlin.GetValue(tileCenter);
			if (value > 1.5f && !AtCoats(planetTile.tileId))
			{
				return PermaIceScore(tile) + 0.01f;
			}
			return -100f;
		}

		public static float PermaIceScore(Tile tile)
		{
			return -20f + ((0f - tile.temperature) * 2f);
		}

		public static bool AtCoats(int tileID)
		{
			var neighbours = new List<PlanetTile>();
			Find.WorldGrid.GetTileNeighbors(tileID, neighbours);
			foreach (int neighbourTile in neighbours)
			{
				try
				{
					if (Find.WorldGrid[neighbourTile].WaterCovered || Find.WorldGrid[neighbourTile].biome == BiomeDefOf.Ocean || Find.World.CoastDirectionAt(tileID) != Rot4.Invalid)
					{
						return true;
					}
				}
				catch { }
				;
			}
			return false;
		}
	}
}
