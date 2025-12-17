using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class CaravanCamp : Camp
    {
        public override string Label
        {
            get
            {
                return IsEmpty ? "RG.EmptyCamp".Translate() : base.Label;
            }
        }

        [Unsaved(false)]
        private Material emptyMaterial;

        [Unsaved(false)]
        private Texture2D emptyExpandingIconTextureInt;

        public override Material Material => IsEmpty ? EmtpyMaterial : base.Material;

        public override Texture2D ExpandingIcon => IsEmpty ? EmtpyExpandingIconTexture : base.ExpandingIcon;

        public SavedCamp savedCamp;

        public Material EmtpyMaterial
        {
            get
            {
                if (emptyMaterial == null)
                {
                    emptyMaterial = MaterialPool.MatFrom(def.texture.Replace("Player", "Empty"), ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue);
                }
                return emptyMaterial;
            }
        }

        public Texture2D EmtpyExpandingIconTexture
        {
            get
            {
                if (emptyExpandingIconTextureInt == null)
                {
                    emptyExpandingIconTextureInt = ContentFinder<Texture2D>.Get(def.expandingIconTexture.Replace("Player", "EmptyPlayer"));
                }
                return emptyExpandingIconTextureInt;
            }
        }

        public bool IsEmpty => HasMap is false;

        public override void PostMapGenerate()
        {
            base.PostMapGenerate();
            if (savedCamp != null)
            {
                RestoreCampMap();
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (IsEmpty && (savedCamp is null || Find.TickManager.TicksGame - savedCamp.ticksSaved
                >= ReGrowthUtils.MakeCampPatchWorker.preserveSavedCampsForDays * GenDate.TicksPerDay))
            {
                Find.WorldObjects.Remove(this);
            }
        }

        public override string GetInspectString()
        {
            if (IsEmpty && savedCamp != null)
            {
                var ticksPassed = Find.TickManager.TicksGame - savedCamp.ticksSaved;
                var ticksTotal = ReGrowthUtils.MakeCampPatchWorker.preserveSavedCampsForDays * GenDate.TicksPerDay;
                return "RG.TimeRemaining".Translate((ticksTotal - ticksPassed).ToStringTicksToPeriod(allowSeconds: false));
            }
            return base.GetInspectString().TrimEndNewlines();
        }

        private void RestoreCampMap()
        {
            SyncGrids(Map.terrainGrid.underGrid, savedCamp.terrainGrid.underGrid, delegate (int i, TerrainDef def)
            {
                if (def is null) return;
                var cell = Map.cellIndices.IndexToCell(i);
                Map.terrainGrid.SetUnderTerrain(cell, def);
            });
            SyncGrids(Map.terrainGrid.foundationGrid, savedCamp.terrainGrid.foundationGrid, delegate (int i, TerrainDef def)
            {
                var cell = Map.cellIndices.IndexToCell(i);
                Map.terrainGrid.SetFoundation(cell, def);
            });
            SyncGrids(Map.terrainGrid.tempGrid, savedCamp.terrainGrid.tempGrid, delegate (int i, TerrainDef def)
            {
                var cell = Map.cellIndices.IndexToCell(i);
                Map.terrainGrid.SetTempTerrain(cell, def);
            });
            SyncGrids(Map.terrainGrid.topGrid, savedCamp.terrainGrid.topGrid, delegate (int i, TerrainDef def)
            {
                var cell = Map.cellIndices.IndexToCell(i);
                Map.terrainGrid.SetTerrain(cell, def);
            });
            SyncGrids(Map.terrainGrid.colorGrid, savedCamp.terrainGrid.colorGrid, delegate (int i, ColorDef def)
            {
                var cell = Map.cellIndices.IndexToCell(i);
                Map.terrainGrid.SetTerrainColor(cell, def);
            });
            DespawnThings(Map.listerThings.AllThings.Where(x => x is Building).ToList());
            DespawnThings(Map.listerThings.AllThings.Where(x => x is Plant).ToList());
            DespawnThings(Map.listerThings.AllThings.Where(x => x.def.category == ThingCategory.Item).ToList());
            SpawnThings(savedCamp.buildings);
            SpawnThings(savedCamp.plants);
            SpawnThings(savedCamp.items);
            var itemsRottables = savedCamp.items.ToDictionary(x => x, x => x.TryGetComp<CompRottable>());
            itemsRottables.RemoveAll(x => x.Value is null);
            var ticksPassed = Find.TickManager.TicksGame - savedCamp.ticksSaved;
            for (var i = 0; i < ticksPassed; i++)
            {
                foreach (var item in itemsRottables)
                {
                    if (item.Value.parent.Destroyed is false)
                    {
                        item.Value.TickInterval(1);
                    }
                }
                var map = Map;
                int num = Mathf.CeilToInt((float)map.Area * 0.0006f);
                int area = map.Area;
                for (int j = 0; j < num; j++)
                {
                    if (Map.steadyEnvironmentEffects.cycleIndex >= area)
                    {
                        Map.steadyEnvironmentEffects.cycleIndex = 0;
                    }
                    IntVec3 c = map.cellsInRandomOrder.Get(Map.steadyEnvironmentEffects.cycleIndex);
                    DoCellSteadyEffects(Map.steadyEnvironmentEffects, c);
                    Map.steadyEnvironmentEffects.cycleIndex++;
                }
            }

            SyncGrids(Map.roofGrid.roofGrid, savedCamp.roofGrid.roofGrid, delegate (int i, RoofDef def)
            {
                var cell = Map.cellIndices.IndexToCell(i);
                Map.roofGrid.SetRoof(cell, def);
            });
        }

        private void DoCellSteadyEffects(SteadyEnvironmentEffects steadyEnvironmentEffects, IntVec3 c)
        {
            var map = steadyEnvironmentEffects.map;
            Room room = c.GetRoom(map);
            bool flag = map.roofGrid.Roofed(c);
            bool flag2 = room?.UsesOutdoorTemperature ?? false;

            if (room != null)
            {
                TerrainDef terrain = c.GetTerrain(map);
                List<Thing> thingList = c.GetThingList(map);
                for (int num = thingList.Count - 1; num >= 0; num--)
                {
                    Thing thing = thingList[num];
                    if (thing is not Filth)
                    {
                        steadyEnvironmentEffects.TryDoDeteriorate(thing, flag, flag2, terrain);
                    }
                }
            }
            map.gameConditionManager.DoSteadyEffects(c, map);
            GasUtility.DoSteadyEffects(c, map);
        }

        private void SpawnThings<T>(List<T> things) where T : Thing
        {
            foreach (var thing in things)
            {
                if (thing.Destroyed)
                {
                    thing.mapIndexOrState = -1;
                }
                GenSpawn.Spawn(thing, thing.Position, Map, thing.Rotation);
            }
        }

        private void DespawnThings(List<Thing> allThings)
        {
            foreach (var thing in allThings)
            {
                thing.DeSpawn();
            }
        }

        private void SyncGrids<T>(T[] mapGrid, T[] savedGrid, Action<int, T> syncAction)
        {
            for (int i = mapGrid.Count() - 1; i >= 0; i--)
            {
                var mapValue = mapGrid[i];
                var savedValue = savedGrid[i];
                if (EqualityComparer<T>.Default.Equals(mapValue, savedValue) is false)
                {
                    syncAction(i, savedValue);
                }
            }
        }

        public override void Notify_MyMapAboutToBeRemoved()
        {
            base.Notify_MyMapAboutToBeRemoved();
            var map = Map;
            savedCamp = new SavedCamp
            {
                buildings = map.listerThings.AllThings.OfType<Building>().ToList(),
                plants = map.listerThings.AllThings.OfType<Plant>().ToList(),
                items = map.listerThings.AllThings.Where(x => x.def.category == ThingCategory.Item).ToList(),
                terrainGrid = map.terrainGrid,
                roofGrid = map.roofGrid,
                ticksSaved = Find.TickManager.TicksGame
            };
        }

        public override void Notify_MyMapRemoved(Map map)
        {
            base.Notify_MyMapRemoved(map);
            var abandonedCamp = Find.WorldObjects.WorldObjectAt<AbandonedCamp>(this.Tile);
            abandonedCamp.savedCamp = savedCamp;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref savedCamp, "savedCamp");
        }
    }
}

