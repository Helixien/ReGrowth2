using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using System.Linq;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(MineAIUtility), nameof(MineAIUtility.JobOnThing))]
    public static class MineAIUtility_JobOnThing_Patch
    {
        public static bool Prefix(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (t.def.size.x == 1 && t.def.size.z == 1)
            {
                return true;
            }

            if (!t.def.mineable)
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationAt(t.Position, DesignationDefOf.Mine) == null && pawn.Map.designationManager.DesignationAt(t.Position, DesignationDefOf.MineVein) == null)
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }

            if (!new HistoryEvent(HistoryEventDefOf.Mined, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job())
            {
                return false;
            }

            bool flag = false;
            foreach (IntVec3 c in t.OccupiedRect().ExpandedBy(1).EdgeCells)
            {
                if (c.InBounds(pawn.Map) && c.Standable(pawn.Map) && ReachabilityImmediate.CanReachImmediate(c, t, pawn.Map, PathEndMode.Touch, pawn))
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                foreach (IntVec3 c in t.OccupiedRect().ExpandedBy(1).EdgeCells)
                {
                    if (!c.InBounds(t.Map) || !ReachabilityImmediate.CanReachImmediate(c, t, pawn.Map, PathEndMode.Touch, pawn) || !c.WalkableBy(t.Map, pawn) || c.Standable(t.Map))
                    {
                        continue;
                    }
                    List<Thing> thingList = c.GetThingList(t.Map);
                    for (int k = 0; k < thingList.Count; k++)
                    {
                        if (thingList[k].def.designateHaulable && thingList[k].def.passability == Traversability.PassThroughOnly)
                        {
                            Job job = HaulAIUtility.HaulAsideJobFor(pawn, thingList[k]);
                            if (job != null)
                            {
                                __result = job;
                                return false;
                            }
                        }
                    }
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                JobFailReason.Is(MineAIUtility.NoPathTrans);
                return false;
            }

            __result = JobMaker.MakeJob(JobDefOf.Mine, t, 20000, checkOverrideOnExpiry: true);
            return false;
        }
    }
}
