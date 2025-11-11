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

        public WallSnowManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
            {
                var buildings = map.listerBuildings.allBuildingsNonColonist.ToList();
                buildings.AddRange(map.listerBuildings.allBuildingsColonist);
                foreach (var building in buildings)
                {
                    if (building.IsWall())
                    {
                        UpdateWallSnowState(building);
                    }
                }
            }
        }

        public void UpdateWallSnowState(Building wall)
        {
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
            if (ShouldHaveSnowIndividual(wall))
            {
                return true;
            }
            var otherWalls = new List<Thing>();
            wall.Map.floodFiller.FloodFill(wall.Position, (IntVec3 x) => x.GetEdifice(wall.Map)?.IsWall() ?? false, delegate (IntVec3 x)
            {
                var edifice = x.GetEdifice(wall.Map);
                if (edifice != null && edifice.IsWall())
                {
                    otherWalls.Add(edifice);
                }
            });
            return otherWalls.Any(x => ShouldHaveSnowIndividual(x));
        }

        private bool ShouldHaveSnowIndividual(Thing wall)
        {
            var terrain = wall.Position.GetTerrain(wall.Map);
            if (terrain.IsSubstructure)
            {
                return false;
            }
            foreach (var adjCell in GenRadial.RadialCellsAround(wall.Position, 1f, true))
            {
                if (adjCell.InBounds(wall.Map) && WeatherBuildupUtility.GetBuildupCategory(wall.Map.snowGrid.GetDepth(adjCell)) == WeatherBuildupCategory.Thick)
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
    }
}
