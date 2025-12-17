using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ReGrowthCore
{
    public class IncidentWorker_DevilDusts : IncidentWorker
    {
        public override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            int devilDustCount = Rand.RangeInclusive(3, 5);
            var devilDusts = new List<Thing>();
            var spawnCenter = CellFinderLoose.RandomCellWith((IntVec3 c) => c.Standable(map) && !c.Roofed(map) && !c.Fogged(map) && !map.areaManager.Home.ActiveCells.Contains(c), map);
            var takenCells = new List<IntVec3>();
            for (int i = 0; i < devilDustCount; i++)
            {
                if (CellFinder.TryFindRandomCellNear(spawnCenter, map, 50, (IntVec3 c) => c.Standable(map) && !c.Roofed(map) && !c.Fogged(map)
                && !map.areaManager.Home.ActiveCells.Contains(c) && !takenCells.Contains(c), out var result))
                {
                    var devilDust = ThingMaker.MakeThing(RG_DefOf.RG_DustDevil);
                    GenSpawn.Spawn(devilDust, result, map);
                    devilDusts.Add(devilDust);
                    takenCells.Add(result);
                }
            }
            SendStandardLetter(parms, devilDusts);
            return true;
        }
    }
}

