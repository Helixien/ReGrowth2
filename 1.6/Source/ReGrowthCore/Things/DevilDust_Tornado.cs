using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace ReGrowthCore
{
	[StaticConstructorOnStartup]
	public class DevilDust_Tornado : ThingWithComps
	{
		private Vector2 realPosition;

		private float direction;

		private int spawnTick;

		private int leftFadeOutTicks = -1;

		private int ticksLeftToDisappear = -1;

		private Sustainer sustainer;

		private static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

		private static ModuleBase directionNoise;

		private const float Wind = 5f;

		private const int CloseDamageIntervalTicks = 15;

		private const int RoofDestructionIntervalTicks = 20;

		private const float FarDamageMTBTicks = 15f;

		private const float CloseDamageRadius = 4.2f;

		private const float FarDamageRadius = 10f;

		private const float BaseDamage = 30f;

		private const int SpawnMoteEveryTicks = 4;

		private static readonly IntRange DurationTicks = new IntRange(2700, 10080);

		private const float DownedPawnDamageFactor = 0.2f;

		private const float AnimalPawnDamageFactor = 0.75f;

		private const float BuildingDamageFactor = 0.8f;

		private const float PlantDamageFactor = 1.7f;

		private const float ItemDamageFactor = 0.68f;

		private const float CellsPerSecond = 1.7f;

		private const float DirectionChangeSpeed = 0.78f;

		private const float DirectionNoiseFrequency = 0.002f;

		private const float TornadoAnimationSpeed = 25f;

		private const float ThreeDimensionalEffectStrength = 4f;

		private const int FadeInTicks = 120;

		private const int FadeOutTicks = 120;

		private const float MaxMidOffset = 2f;

		private static readonly Material TornadoMaterial = MaterialPool.MatFrom("Things/Ethereal/DustDevil", ShaderDatabase.Transparent, MapMaterialRenderQueues.Tornado);

		private static readonly FloatRange PartsDistanceFromCenter = new FloatRange(1f, 2f);

		private static readonly float ZOffsetBias = -4f * PartsDistanceFromCenter.min;

		private List<IntVec3> removedRoofsTmp = new List<IntVec3>();

		private static List<Thing> tmpThings = new List<Thing>();

		private float FadeInOutFactor
		{
			get
			{
				float a = Mathf.Clamp01((float)(Find.TickManager.TicksGame - spawnTick) / 120f);
				float b = (leftFadeOutTicks < 0) ? 1f : Mathf.Min((float)leftFadeOutTicks / 120f, 1f);
				return Mathf.Min(a, b);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref realPosition, "realPosition");
			Scribe_Values.Look(ref direction, "direction", 0f);
			Scribe_Values.Look(ref spawnTick, "spawnTick", 0);
			Scribe_Values.Look(ref leftFadeOutTicks, "leftFadeOutTicks", 0);
			Scribe_Values.Look(ref ticksLeftToDisappear, "ticksLeftToDisappear", 0);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				Vector3 vector = base.Position.ToVector3Shifted();
				realPosition = new Vector2(vector.x, vector.z);
				direction = Rand.Range(0f, 360f);
				spawnTick = Find.TickManager.TicksGame;
				leftFadeOutTicks = -1;
				ticksLeftToDisappear = DurationTicks.RandomInRange;
			}
			CreateSustainer();
		}

		public override void Tick()
		{
			if (!base.Spawned)
			{
				return;
			}
			if (sustainer == null)
			{
				Log.Error("Tornado sustainer is null.");
				CreateSustainer();
			}
			sustainer.Maintain();
			UpdateSustainerVolume();
			GetComp<CompWindSource>().wind = 5f * FadeInOutFactor;
			if (leftFadeOutTicks > 0)
			{
				leftFadeOutTicks--;
				if (leftFadeOutTicks == 0)
				{
					Destroy();
				}
				return;
			}
			if (directionNoise == null)
			{
				directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, 1948573612, QualityMode.Medium);
			}
			direction += (float)directionNoise.GetValue(Find.TickManager.TicksAbs, (float)(thingIDNumber % 500) * 1000f, 0.0) * 0.78f; ;
			var movedPosition = realPosition.Moved(direction, 17f / 600f);
			var cell = new Vector3(movedPosition.x, 0f, movedPosition.y).ToIntVec3();
			if (this.Map is null)
			{
				Destroy();
				return;
			}
			if (cell.IsValid && this.Map != null)
			{
				if (!cell.InBounds(this.Map) || !cell.Roofed(this.Map))
				{
					realPosition = movedPosition;
				}
			}

			IntVec3 intVec = new Vector3(realPosition.x, 0f, realPosition.y).ToIntVec3();
			if (intVec.InBounds(base.Map))
			{
				base.Position = intVec;
				if (this.IsHashIntervalTick(15))
				{
					DamageCloseThings();
				}
				if (Rand.MTBEventOccurs(15f, 1f, 1f))
				{
					DamageFarThings();
				}
				//if (this.IsHashIntervalTick(20))
				//{
				//	DestroyRoofs();
				//}
				if (ticksLeftToDisappear > 0)
				{
					ticksLeftToDisappear--;
					if (ticksLeftToDisappear == 0)
					{
						leftFadeOutTicks = 120;
						Messages.Message("RG.SandDevilDissipated".Translate(), new TargetInfo(base.Position, base.Map), MessageTypeDefOf.PositiveEvent);
					}
				}
				if (this.IsHashIntervalTick(4) && !CellImmuneToDamage(base.Position))
				{
					//float num = Rand.Range(0.6f, 1f);
					Vector3 a = new Vector3(realPosition.x, 0f, realPosition.y);
					a.y = AltitudeLayer.MoteOverhead.AltitudeFor();
					ThrowDevilDustPuff(a + Vector3Utility.RandomHorizontalOffset(1.5f), base.Map, Rand.Range(1.5f, 3f), new ColorInt(191, 161, 127).ToColor);
				}
			}
			else
			{
				leftFadeOutTicks = 120;
				Messages.Message("RG.SandDevilLeftTheMap".Translate(), new TargetInfo(base.Position, base.Map), MessageTypeDefOf.PositiveEvent);
			}
		}

		public static void ThrowDevilDustPuff(Vector3 loc, Map map, float scale, Color color)
		{
			if (loc.ShouldSpawnMotesAt(map) && !map.moteCounter.SaturatedLowPriority)
			{
				MoteThrown obj = (MoteThrown)ThingMaker.MakeThing(RG_DefOf.RG_Mote_DevilDustPuff);
				obj.Scale = 1.9f * scale;
				obj.rotationRate = Rand.Range(-60, 60);
				obj.exactPosition = loc;
				obj.instanceColor = color;
				obj.SetVelocity(Rand.Range(0, 360), Rand.Range(0.6f, 0.75f));
				GenSpawn.Spawn(obj, loc.ToIntVec3(), map);
			}
		}

		public override void DrawAt(global::UnityEngine.Vector3 drawLoc, bool flip = false)
		{
			Rand.PushState();
			Rand.Seed = thingIDNumber;
			for (int i = 0; i < 90; i++)
			{
				DrawTornadoPart(PartsDistanceFromCenter.RandomInRange, Rand.Range(0f, 360f), Rand.Range(0.9f, 1.1f), Rand.Range(0.52f, 0.88f));
			}
			Rand.PopState();
		}

		private void DrawTornadoPart(float distanceFromCenter, float initialAngle, float speedMultiplier, float colorMultiplier)
		{
			int ticksGame = Find.TickManager.TicksGame;
			float num = 1f / distanceFromCenter;
			float num2 = 5f * speedMultiplier * num;
			float num3 = (initialAngle + (float)ticksGame * num2) % 360f;
			Vector2 vector = realPosition.Moved(num3, AdjustedDistanceFromCenter(distanceFromCenter));
			vector.y += distanceFromCenter * 4f;
			vector.y += ZOffsetBias;
			Vector3 a = new Vector3(vector.x, AltitudeLayer.Weather.AltitudeFor() + 3f / 70f * Rand.Range(0f, 1f), vector.y);
			float num4 = distanceFromCenter * 3f;
			float num5 = 1f;
			if (num3 > 270f)
			{
				num5 = GenMath.LerpDouble(270f, 360f, 0f, 1f, num3);
			}
			else if (num3 > 180f)
			{
				num5 = GenMath.LerpDouble(180f, 270f, 1f, 0f, num3);
			}
			float num6 = Mathf.Min(distanceFromCenter / (PartsDistanceFromCenter.max + 2f), 1f);
			float d = Mathf.InverseLerp(0.18f, 0.4f, num6);
			Vector3 a2 = new Vector3(Mathf.Sin((float)ticksGame / 1000f + (float)(thingIDNumber * 10)) * 2f, 0f, 0f);
			Vector3 pos = a + a2 * d;
			float a3 = Mathf.Max(1f - num6, 0f) * num5 * FadeInOutFactor;
			Color value = new Color(colorMultiplier, colorMultiplier, colorMultiplier, a3);
			matPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
			Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, num3, 0f), new Vector3(num4, 1f, num4));
			Graphics.DrawMesh(MeshPool.plane10, matrix, TornadoMaterial, 0, null, 0, matPropertyBlock);
		}

		private float AdjustedDistanceFromCenter(float distanceFromCenter)
		{
			float num = Mathf.Min(distanceFromCenter / 8f, 1f);
			num *= num;
			return distanceFromCenter * num;
		}

		private void UpdateSustainerVolume()
		{
			sustainer.info.volumeFactor = FadeInOutFactor;
		}

		private void CreateSustainer()
		{
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				SoundDef tornado = RG_DefOf.RG_Ambient_DustDevil;
				sustainer = tornado.TrySpawnSustainer(SoundInfo.InMap(this));
				UpdateSustainerVolume();
			});
		}

		private void DamageCloseThings()
		{
			int num = GenRadial.NumCellsInRadius(1.2f);
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
				if (intVec.InBounds(base.Map) && !CellImmuneToDamage(intVec))
				{
					Pawn firstPawn = intVec.GetFirstPawn(base.Map);
					if (firstPawn == null || !firstPawn.Downed || !Rand.Bool)
					{
						float damageFactor = GenMath.LerpDouble(0f, 4.2f, 1f, 0.2f, intVec.DistanceTo(base.Position));
						DoDamage(intVec, damageFactor);
					}
				}
			}
		}

		private void DamageFarThings()
		{
			IntVec3 c = (from x in GenRadial.RadialCellsAround(base.Position, 4, useCenter: true)
						 where x.InBounds(base.Map)
						 select x).RandomElement();
			if (!CellImmuneToDamage(c))
			{
				DoDamage(c, 0.5f);
			}
		}

		//private void DestroyRoofs()
		//{
		//	removedRoofsTmp.Clear();
		//	foreach (IntVec3 item in from x in GenRadial.RadialCellsAround(base.Position, 4.2f, useCenter: true)
		//							 where x.InBounds(base.Map)
		//							 select x)
		//	{
		//		if (!CellImmuneToDamage(item) && item.Roofed(base.Map))
		//		{
		//			RoofDef roof = item.GetRoof(base.Map);
		//			if (!roof.isThickRoof && !roof.isNatural)
		//			{
		//				RoofCollapserImmediate.DropRoofInCells(item, base.Map);
		//				removedRoofsTmp.Add(item);
		//			}
		//		}
		//	}
		//	if (removedRoofsTmp.Count > 0)
		//	{
		//		RoofCollapseCellsFinder.CheckCollapseFlyingRoofs(removedRoofsTmp, base.Map, removalMode: true);
		//	}
		//}

		private bool CellImmuneToDamage(IntVec3 c)
		{
			if (c.Roofed(base.Map) && c.GetRoof(base.Map).isThickRoof)
			{
				return true;
			}
			Building edifice = c.GetEdifice(base.Map);
			if (edifice != null && edifice.def.category == ThingCategory.Building && (edifice.def.building.isNaturalRock || (edifice.def == ThingDefOf.Wall && edifice.Faction == null)))
			{
				return true;
			}
			return false;
		}

		private void DoDamage(IntVec3 c, float damageFactor)
		{
			tmpThings.Clear();
			tmpThings.AddRange(c.GetThingList(base.Map));
			Vector3 vector = c.ToVector3Shifted();
			float angle = 0f - Vector2Utility.AngleTo(b: new Vector2(vector.x, vector.z), a: realPosition) + 180f;
			for (int i = 0; i < tmpThings.Count; i++)
			{
				BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = null;
				switch (tmpThings[i].def.category)
				{
					case ThingCategory.Pawn:
						{
							Pawn pawn = (Pawn)tmpThings[i];
							battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Tornado);
							Find.BattleLog.Add(battleLogEntry_DamageTaken);
							if (pawn.RaceProps.baseHealthScale < 1f)
							{
								damageFactor *= pawn.RaceProps.baseHealthScale;
							}
							if (pawn.RaceProps.Animal)
							{
								damageFactor *= 0.75f;
							}
							if (pawn.Downed)
							{
								damageFactor *= 0.2f;
							}
							break;
						}
					case ThingCategory.Building:
						damageFactor = 0f;
						break;
					case ThingCategory.Item:
						damageFactor = 0f;
						break;
					case ThingCategory.Plant:
						damageFactor *= 1.7f;
						break;
				}
				tmpThings[i].TakeDamage(new DamageInfo(DamageDefOf.TornadoScratch, 1f * damageFactor, 0f, angle, this)).AssociateWithLog(battleLogEntry_DamageTaken);
			}
			tmpThings.Clear();
		}
	}
}

