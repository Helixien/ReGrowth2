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
        private int currentBatchIndex = 0;
        private const int WALLS_PER_TICK = 50;

        public WallSnowManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!ReGrowthUtils.SnowOnWallsPatchWorker.snowOnWalls)
                return;

            if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
            {
                int currentTick = Find.TickManager.TicksGame;
                if (currentTick - lastFullScanTick > FULL_SCAN_INTERVAL)
                {
                    RebuildEligibleWallsCache();
                    lastFullScanTick = currentTick;
                }
                ProcessWallBatch();
            }
        }

        private void RebuildEligibleWallsCache()
        {
            eligibleWalls.Clear();
            wallsToProcess.Clear();
            var allWalls = map.listerBuildings.allBuildingsColonist
                .Concat(map.listerBuildings.allBuildingsNonColonist)
                .Where(b => b.def.IsWall);

            foreach (var wall in allWalls)
            {
                if (IsWallOutside(wall))
                {
                    eligibleWalls.Add(wall);
                    wallsToProcess.Add(wall);
                }
            }

            currentBatchIndex = 0;
        }

        private void ProcessWallBatch()
        {
            if (wallsToProcess.Count == 0)
            {
                RebuildEligibleWallsCache();
                return;
            }

            int endIndex = Mathf.Min(currentBatchIndex + WALLS_PER_TICK, wallsToProcess.Count);

            for (int i = currentBatchIndex; i < endIndex; i++)
            {
                var wall = wallsToProcess[i];
                if (wall == null || wall.Destroyed || !eligibleWalls.Contains(wall))
                    continue;

                UpdateWallSnowState(wall);
            }

            currentBatchIndex = endIndex;
            if (currentBatchIndex >= wallsToProcess.Count)
            {
                currentBatchIndex = 0;
            }
        }
        private Dictionary<Building, bool> wallGroupSnowCache = new Dictionary<Building, bool>();
        private int lastSnowCheckTick = -1;

        public void UpdateWallSnowState(Building wall)
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
        private Dictionary<IntVec3, bool> outsideCache = new Dictionary<IntVec3, bool>();

        private bool IsWallOutside(Building wall)
        {
            if (outsideCache.TryGetValue(wall.Position, out bool cached))
                return cached;

            bool isOutside = GenRadial.RadialCellsAround(wall.Position, 1f, true)
                .Any(c => c.InBounds(map) && c.UsesOutdoorTemperature(map));

            outsideCache[wall.Position] = isOutside;
            return isOutside;
        }

        private bool ShouldHaveSnowIndividual(Building wall)
        {
            foreach (var adjCell in GenRadial.RadialCellsAround(wall.Position, 1f, true))
            {
                if (adjCell.InBounds(map) &&
                    WeatherBuildupUtility.GetBuildupCategory(map.snowGrid.GetDepth(adjCell)) == WeatherBuildupCategory.Thick)
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
                outsideCache ??= new Dictionary<IntVec3, bool>();
            }
        }
    }
}
