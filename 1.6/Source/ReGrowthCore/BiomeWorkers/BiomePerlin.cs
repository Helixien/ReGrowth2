using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ReGrowthCore
{
	public static class BiomePerlin
	{
		private static Dictionary<BiomeDef, Multiply> noisesPerBiomes = new Dictionary<BiomeDef, Multiply>();
		public static Multiply GetNoiseFor(BiomeDef biomeDef, float freqMultiplier = 1f)
		{
			if (!noisesPerBiomes.TryGetValue(biomeDef, out var value))
			{
				value = SetupBiomeNoise(biomeDef, freqMultiplier);
				noisesPerBiomes[biomeDef] = value;
			}
			return value;
		}
		private static Multiply SetupBiomeNoise(BiomeDef biomeDef, float freqMultiplier)
		{
			Rand.PushState();
			Rand.Seed = biomeDef.GetHashCode();
			ModuleBase moduleBase = new Perlin((double)(0.09f * freqMultiplier), 2.0, 0.40000000596046448, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
			ModuleBase moduleBase2 = new RidgedMultifractal((double)(0.025f * freqMultiplier), 2.0, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
			moduleBase = new ScaleBias(0.5, 0.5, moduleBase);
			moduleBase2 = new ScaleBias(0.5, 0.5, moduleBase2);
			var noiseElevation = new Multiply(moduleBase, moduleBase2);
			InverseLerp inverseLerp = new InverseLerp(noiseElevation, ElevationRange.max, ElevationRange.min);
			noiseElevation = new Multiply(noiseElevation, inverseLerp);
			NoiseDebugUI.StorePlanetNoise(noiseElevation, "noiseBiome" + biomeDef.defName);
			Rand.PopState();
			return noiseElevation;
		}

		private static readonly FloatRange ElevationRange = new FloatRange(650f, 750f);
	}
}
