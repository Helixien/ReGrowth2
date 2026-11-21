using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    public class WallSnowManager : MapComponent
    {
        public HashSet<Thing> snowCoveredWalls = new HashSet<Thing>();
        private Graphic snowOverlayGraphic;
        private HashSet<Building> eligibleWalls = new HashSet<Building>();
        private int lastFullScanTick = -1;
        private const int FULL_SCAN_INTERVAL = 2500;
        private List<Building> wallsToProcess = new List<Building>();
        private readonly HashSet<Building> pendingWalls = new HashSet<Building>();
        private int currentBatchIndex = 0;
        private bool batchPassComplete = true;
        private const int WALLS_PER_TICK = 50;

        public WallSnowManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!ReGrowthUtils.SnowOnWallsPatchWorker.snowOnWalls ||
                !map.IsHashIntervalTick(GenTicks.TickRareInterval) ||
                !map.regionAndRoomUpdater.Enabled)
                return;

            outdoorTemperatureByCell.Clear();

            int currentTick = Find.TickManager.TicksGame;
            if (batchPassComplete && (lastFullScanTick < 0 || currentTick - lastFullScanTick > FULL_SCAN_INTERVAL))
            {
                RebuildEligibleWallsCache();
                lastFullScanTick = currentTick;
            }

            if (pendingWalls.Count > 0)
            {
                wallGroupSnowCache.Clear();
                foreach (var wall in pendingWalls)
                {
                    if (IsSpawnedWall(wall))
                        AddWallToBatch(wall);
                }
                pendingWalls.Clear();
            }

            ProcessWallBatch();
        }

        private void RebuildEligibleWallsCache()
        {
            eligibleWalls.Clear();
            wallsToProcess.Clear();

            foreach (var wallDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!wallDef.IsWall)
                    continue;

                foreach (var wallThing in map.listerThings.ThingsOfDef(wallDef))
                {
                    if (wallThing is Building wall && IsWallOutside(wall))
                    {
                        AddWallToBatch(wall);
                    }
                }
            }

            snowCoveredWalls.RemoveWhere(thing => !IsSpawnedWall(thing));
            foreach (var wall in snowCoveredWalls)
            {
                AddWallToBatch((Building)wall);
            }

            currentBatchIndex = 0;
        }

        private bool IsSpawnedWall(Thing thing)
        {
            return thing is Building { Spawned: true } wall && wall.Map == map && wall.def.IsWall;
        }

        private void AddWallToBatch(Building wall)
        {
            if (eligibleWalls.Add(wall))
                wallsToProcess.Add(wall);
        }

        private void ProcessWallBatch()
        {
            int endIndex = Mathf.Min(currentBatchIndex + WALLS_PER_TICK, wallsToProcess.Count);

            for (int i = currentBatchIndex; i < endIndex; i++)
            {
                var wall = wallsToProcess[i];
                if (!IsSpawnedWall(wall))
                    continue;

                ApplyWallSnowState(wall);
            }

            currentBatchIndex = endIndex;
            batchPassComplete = currentBatchIndex >= wallsToProcess.Count;
            if (batchPassComplete)
            {
                currentBatchIndex = 0;
            }
        }
        private Dictionary<Building, bool> wallGroupSnowCache = new Dictionary<Building, bool>();
        private int lastSnowCheckTick = -1;

        public void UpdateWallSnowState(Building wall)
        {
            if (ReGrowthUtils.SnowOnWallsPatchWorker?.snowOnWalls == true)
                pendingWalls.Add(wall);
        }

        private void ApplyWallSnowState(Building wall)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - lastSnowCheckTick > 2500)
            {
                wallGroupSnowCache.Clear();
                lastSnowCheckTick = currentTick;
            }

            bool shouldBeSnowCovered = ShouldHaveSnow(wall);
            if (shouldBeSnowCovered && !snowCoveredWalls.Contains(wall))
            {
                snowCoveredWalls.Add(wall);
                map.mapDrawer.MapMeshDirty(wall.Position, MapMeshFlagDefOf.Things);
            }
            else if (!shouldBeSnowCovered && snowCoveredWalls.Contains(wall))
            {
                snowCoveredWalls.Remove(wall);
                map.mapDrawer.MapMeshDirty(wall.Position, MapMeshFlagDefOf.Things);
            }
        }

        private bool ShouldHaveSnow(Building wall)
        {
            var terrain = map.terrainGrid.TerrainAt(wall.Position);
            if (terrain != null && terrain.IsSubstructure)
            {
                return false;
            }
            if (ShouldHaveSnowIndividual(wall))
            {
                return true;
            }
            if (wallGroupSnowCache.TryGetValue(wall, out bool cachedResult))
            {
                return cachedResult;
            }
            var otherWalls = new List<Building>();
            map.floodFiller.FloodFill(wall.Position,
                (IntVec3 x) => x.GetEdifice(map) is Building b && b.def.IsWall,
                delegate (IntVec3 x)
                {
                    if (x.GetEdifice(map) is Building edifice && edifice.def.IsWall)
                    {
                        otherWalls.Add(edifice);
                    }
                });

            bool result = otherWalls.Any(ShouldHaveSnowIndividual);
            foreach (var w in otherWalls)
            {
                wallGroupSnowCache[w] = result;
            }

            return result;
        }
        private readonly Dictionary<IntVec3, bool> outdoorTemperatureByCell = new Dictionary<IntVec3, bool>();

        private bool IsWallOutside(Building wall)
        {
            return GenRadial.RadialCellsAround(wall.Position, 1f, true)
                .Any(cell => cell.InBounds(map) && CellUsesOutdoorTemperature(cell));
        }

        private bool CellUsesOutdoorTemperature(IntVec3 cell)
        {
            if (!outdoorTemperatureByCell.TryGetValue(cell, out bool usesOutdoorTemperature))
            {
                usesOutdoorTemperature = cell.UsesOutdoorTemperature(map);
                outdoorTemperatureByCell.Add(cell, usesOutdoorTemperature);
            }
            return usesOutdoorTemperature;
        }

        private bool ShouldHaveSnowIndividual(Building wall)
        {
            foreach (var adjCell in GenRadial.RadialCellsAround(wall.Position, 1f, true))
            {
                if (adjCell.InBounds(map) &&
                    WeatherBuildupUtility.GetBuildupCategory(map.snowGrid.GetDepth(adjCell)) == WeatherBuildupCategory.Thick &&
                    CellUsesOutdoorTemperature(adjCell))
                {
                    return true;
                }
            }
            return false;
        }

        public Graphic GetSnowOverlayGraphic()
        {
            if (snowOverlayGraphic == null)
            {
                var graphicData = new GraphicData();
                graphicData.CopyFrom(ThingDefOf.Wall.graphicData);
                graphicData.graphicClass = typeof(Graphic_Single);
                graphicData.linkType = LinkDrawerType.CornerFiller;
                graphicData.linkFlags = LinkFlags.Wall;
                graphicData.texPath = "Things/Building/Linked/Wall_SnowOverlay";
                graphicData.color = Color.white;
                snowOverlayGraphic = graphicData.Graphic;
            }
            return snowOverlayGraphic;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref snowCoveredWalls, "snowCoveredWalls", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                snowCoveredWalls ??= new HashSet<Thing>();
                eligibleWalls ??= new HashSet<Building>();
                wallsToProcess ??= new List<Building>();
                wallGroupSnowCache ??= new Dictionary<Building, bool>();
            }
        }
    }
}
