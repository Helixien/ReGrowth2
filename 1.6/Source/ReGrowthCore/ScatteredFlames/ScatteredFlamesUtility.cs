using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace ReGrowthCore
{
	[StaticConstructorOnStartup]
	public static class ScatteredFlamesUtility
	{
		public static Dictionary<int, FlameData> fireCache = new Dictionary<int, FlameData>();
		public static HashSet<IntVec3> burningCache = new HashSet<IntVec3>();
		public static bool nextFrame, smokeInstalled, somethingBurning;
		public static int triggeringFrameID, curTimeSpeed;
		static DefModExtension backup;
		public static FastRandom fastRandom;
		static ScatteredFlamesUtility()
		{
			Setup();
		}

		public static void Setup()
		{
			fastRandom = new FastRandom();
			smokeInstalled = LoadedModManager.RunningMods.Any(x => x.Name == "Simple FX: Smoke");
			if (Prefs.DevMode && smokeInstalled) Log.Message("[Scattered Flames] Integrated with Simple FX: Smoke.");
			ThingDef fire = ThingDefOf.Fire;
			if (fire == null)
			{
				Log.Warning("[Scattered Flames] Vanilla fire definition not found.");
				return;
			}

			//Remove smoke extension?
			bool restartNeeded = false;
			if (!ReGrowthCore_ScatteredFlames.ModSettings.smoke)
			{
				backup = fire.modExtensions?.FirstOrDefault(x => x.GetType().Name == "Flecker");
				if (backup != null && fire.modExtensions.Remove(backup)) restartNeeded = true;
			}
			else if (fire.modExtensions == null && backup != null)
			{
				fire.modExtensions = new List<DefModExtension>();
				fire.modExtensions.Add(backup);
			}
			else if (fire.modExtensions?.FirstOrDefault(x => x.GetType().Name == "Flecker") == null && backup != null)
			{
				fire.modExtensions.Add(backup);
				restartNeeded = true;
			}

			//Reload save?
			if (restartNeeded && Current.ProgramState == ProgramState.Playing)
			{
				Find.WindowStack.Add(new Dialog_MessageBox("ScatteredFlames.ReloadRequired".Translate(), null, null, null, null, "ScatteredFlames.ReloadHeader".Translate(), true, null, null, WindowLayer.Dialog));
			}
		}

		public class FlameData
		{
			public FlameData(Fire fire)
			{
				this.fire = fire;
				//Determine starting frame
				frame = fastRandom.Next(0,3);

				//Determine if multiflame or not
				numOfOffsets = ReGrowthCore_ScatteredFlames.ModSettings.multiFlames ? fastRandom.Next(1,3) : 1;

				//Determine offsets
				offsets = new Vector3[numOfOffsets];
				if (numOfOffsets == 1)
				{
					offsets[0] = new Vector3(fastRandom.Next(-250,250) / 1000f, 0, fastRandom.Next(-250,250) / 1000f);
					maxFireSize = fastRandom.Next(1300,1850) / 1000f;
				}
				else
				{
					offsets[0] = new Vector3(fastRandom.Next(-400,-200) / 1000f, 0, fastRandom.Next(-400, 400) / 1000f);
					offsets[1] = new Vector3(fastRandom.Next(200, 400) / 1000f, 0, fastRandom.Next(-400, 400) / 1000f);
					maxFireSize = fastRandom.Next(700,1000) / 1000f;
				}

				//Cache matrix and compensate offsets
				matrix = new Matrix4x4[numOfOffsets];
				for (int i = numOfOffsets; i-- > 0;)
				{
					offsets[i] += fire.Position.ToVector3() + new Vector3(0.5f, 0, 0.5f);
					matrix[i].SetTRS(new Vector3(offsets[i].x, fire.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.PawnState).y, offsets[i].z), Quaternion.identity, new Vector3(fire.fireSize, 1f, fire.fireSize));
				}

				//Roofed?
				roofed = fire.Map.roofGrid.Roofed(fire.Position);

				//Hash offset
				hashOffset = fastRandom.Next(10);
			}
			
			public Fire fire;
			public int frame, numOfOffsets;
			public Vector3[] offsets;
			public Matrix4x4[] matrix;
			public float maxFireSize;
			public bool roofed;
			public int hashOffset;
		}
	
		public static void ThrowLongFireGlow(Vector3 c, Map map, float size)
		{
			if (!c.ShouldSpawnMotesAt(map)) return;
			
			Vector3 vector = c + size * new Vector3(fastRandom.Next(1, 100) / 100f - 0.5f, 0f, fastRandom.Next(1, 100) / 100f - 0.5f);
			if (!vector.InBounds(map)) return;

			FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, ScatteredFlames_ResourceBank.FleckDefOf.RG_LongFireGlow, fastRandom.Next(400, 600) / 100f * size);
			dataStatic.rotationRate = fastRandom.Next(-300, 300) / 100f;
			dataStatic.velocityAngle = fastRandom.Next(360) / 100f;
			dataStatic.velocitySpeed = 0.12f;
			dataStatic.def.graphicData.color *= 0.5f;
			map.flecks.CreateFleck(dataStatic);
		}
	}
}