using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ReGrowthCore
{
    public class SavedCamp : IExposable
    {
        public int ticksSaved;
        public List<Building> buildings;
        public List<Plant> plants;
        public TerrainGrid terrainGrid;
        public RoofGrid roofGrid;
        public List<Thing> items;

        public void ExposeData()
        {
            FixLists();
            var fakeMap = new Map();
            var camp = new CaravanCamp();
            fakeMap.generationTick = GenTicks.TicksGame;
            fakeMap.info.Size = WorldObjectDefOf.Camp.overrideMapSize ?? Find.World.info.initialMapSize;
            fakeMap.info.parent = camp;
            fakeMap.events = new MapEvents(fakeMap);
            fakeMap.ConstructComponents();
            Scribe_Values.Look(ref ticksSaved, "ticksSaved");
            Scribe_Collections.Look(ref buildings, "buildings", LookMode.Deep);
            Scribe_Collections.Look(ref plants, "plants", LookMode.Deep);
            Scribe_Collections.Look(ref items, "items", LookMode.Deep);
            Scribe_Deep.Look(ref roofGrid, "roofGrid", fakeMap);
            Scribe_Deep.Look(ref terrainGrid, "terrainGrid", fakeMap);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                buildings ??= new List<Building>();
                plants ??= new List<Plant>();
                items ??= new List<Thing>();
            }
        }

        public void FixLists()
        {
            buildings?.ForEach(x => x.mapIndexOrState = -1);
            plants?.ForEach(x => x.mapIndexOrState = -1);
            items?.ForEach(x => x.mapIndexOrState = -1);
        }
    }
}

