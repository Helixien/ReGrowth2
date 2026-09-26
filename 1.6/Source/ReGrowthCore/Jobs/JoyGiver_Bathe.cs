using RimWorld;
using System;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace ReGrowthCore
{
    public class JoyGiver_Bathe : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (!CanBathe(pawn))
            {
                return null;
            }

            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            var result = FindSpotToBathe(pawn);
            if (result.IsValid)
            {
                return JobMaker.MakeJob(RG_DefOf.RG_Bathe, result);
            }
            return null;
        }

        public static bool CanBathe(Pawn pawn)
        {
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var type = apparel.def.GetType();
                    if (type.Name.Contains("WarcasketDef"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static FloatRange GetComfortTempRange(Pawn pawn)
        {
            var comfortRange = pawn != null ? ComfortableTemperatureRange(pawn) : GenTemperature.ComfortableTemperatureRange(ThingDefOf.Human);
            comfortRange.min -= 10f;
            comfortRange.max += 10f;
            return comfortRange;
        }

        public static bool IsGoodSpotForBathing(Map map, IntVec3 cell, FloatRange comfortRange,
            StringBuilder failReason = null, Pawn pawn = null)
        {
            if (cell.GetZone(map) is not Zone_Bathe)
            {
                failReason?.Append("RG.BathingZoneRemoved".Translate());
                return false;
            }

            if (pawn != null && cell.InAllowedArea(pawn) is false)
            {
                return false;
            }

            if (cell.UsesOutdoorTemperature(map) && !JoyUtility.EnjoyableOutsideNow(map, failReason))
            {
                return false;
            }
            float temp = cell.GetTemperature(map);
            if (!comfortRange.Includes(temp))
            {
                var terrain = cell.GetTerrain(map);
                bool isHotSpring = terrain.IsHotSpring();
                if (comfortRange.min > temp && isHotSpring is false)
                {
                    failReason?.Append("RG.TooCold".Translate());
                    return false;
                }
                else if (temp > comfortRange.max && isHotSpring)
                {
                    failReason?.Append("RG.TooHot".Translate());
                    return false;
                }
            }
            return true;
        }

        private static FloatRange ComfortableTemperatureRange(Pawn p)
        {
            var oldApparelTracker = p.apparel;
            p.apparel = new Pawn_ApparelTracker(p);
            var result = new FloatRange(p.GetStatValue(StatDefOf.ComfyTemperatureMin), p.GetStatValue(StatDefOf.ComfyTemperatureMax));
            p.apparel = oldApparelTracker;
            return result;
        }

        public int NearbyWaterCount(IntVec3 cell, Map map)
        {
            int nearbyWaterCount = 0;
            foreach (var adj in GenAdj.AdjacentCells)
            {
                var nearbyCell = adj + cell;
                if (map.terrainGrid.waterCells.Contains(nearbyCell))
                {
                    nearbyWaterCount++;
                }
            }
            return nearbyWaterCount;
        }
        public IntVec3 FindSpotToBathe(Pawn pawn)
        {
            var comfortRange = GetComfortTempRange(pawn);
            bool CellValidator(IntVec3 x)
            {
                if (IsGoodSpotForBathing(pawn.Map, x, comfortRange, pawn: pawn) && pawn.CanReserve(x))
                {
                    foreach (var adj in GenAdj.AdjacentCells)
                    {
                        var nearbyCell = adj + x;
                        if (nearbyCell.GetZone(pawn.Map) is Zone_Bathe && pawn.CanReserve(nearbyCell) is false)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            var cells = pawn.Map.terrainGrid.waterCells.Where(x => CellValidator(x)).OrderByDescending(x => NearbyWaterCount(x, pawn.Map))
                .ThenBy(x => pawn.Position.DistanceTo(x)).Take(10);
            if (cells.TryRandomElement(out var cell))
            {
                return cell;
            }
            return IntVec3.Invalid;
        }
    }
}
