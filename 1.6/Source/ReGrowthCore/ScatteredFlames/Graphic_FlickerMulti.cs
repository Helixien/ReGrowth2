using UnityEngine;
using UnityEngine.Rendering;
using Verse;
using RimWorld;
using static ReGrowthCore.ScatteredFlamesUtility;

namespace ReGrowthCore
{
	public class Graphic_FlickerMulti : Graphic_Flicker
	{
		public Graphic_FlickerMulti() {}

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			if (this.subGraphics == null || !fireCache.TryGetValue(thing.thingIDNumber, out ScatteredFlamesUtility.FlameData subFlame)) return;

			var totalFrames = base.subGraphics.Length;
			if (curTimeSpeed != 0 && nextFrame && triggeringFrameID == RealTime.frameCount && subFlame.frame-- == 0)
			{
				float fireSize = subFlame.fire.fireSize;
				subFlame.frame = totalFrames - 1;
				for (int i = subFlame.numOfOffsets; i-- > 0;)
				{
					subFlame.matrix[i].m00 = subFlame.matrix[i].m22 = Mathf.Min(subFlame.maxFireSize, fireSize) * ScatteredFlamesUtility.fastRandom.Next(90,110) / 100f;
				}
				if (ReGrowthCore_ScatteredFlames.ModSettings.specialFX && !subFlame.roofed && fireSize > 0.5f && ScatteredFlamesUtility.fastRandom.NextBool())
				{
					if (ScatteredFlamesUtility.fastRandom.Next(3) == 0) FleckMaker.ThrowMicroSparks(loc, thing.Map);
					if (fireSize > 0.75f && ScatteredFlamesUtility.fastRandom.Next(20) == 0) ThrowLongFireGlow(loc, thing.Map, fireSize);
					if (ScatteredFlamesUtility.fastRandom.Next(30) == 0) FleckMaker.ThrowHeatGlow(thing.Position, thing.Map, fireSize);
					if (ScatteredFlamesUtility.fastRandom.Next(5) == 0) FleckMaker.ThrowDustPuffThick(loc, thing.Map, fireSize * 2f, ScatteredFlames_ResourceBank.color);
				}
			}

			for (int i = subFlame.numOfOffsets; i-- > 0;)
			{
			             Graphics.DrawMesh(
			                 MeshPool.plane10,
			                 subFlame.matrix[i],
			                 ((Graphic_Single)this.subGraphics[(subFlame.frame + i) % totalFrames]).mat,
			                 0,
			                 null,
			                 0,
			                 null,
			                 false,
			                 false,
			                 false
			             );
			}
		}
	}
}