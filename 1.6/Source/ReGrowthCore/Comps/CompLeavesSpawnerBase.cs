using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ReGrowthCore
{
    public class CompLeavesSpawnerBase : ThingComp
    {
        protected int ticksUntilSpawn;
        public CompProperties_Spawner PropsSpawner => (CompProperties_Spawner)props;

        private bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                ResetCountdown();
            }
        }

        public override void CompTick()
        {
            TickInterval(1);
        }

        public override void CompTickRare()
        {
            TickInterval(250);
        }

        public override void CompTickLong()
        {
            TickInterval(2000);
        }

        public virtual bool ShouldSpawn()
        {
            return true;
        }

        private void TickInterval(int interval)
        {
            if (!ShouldSpawn())
            {
                return;
            }
            var comp = parent.GetComp<CompCanBeDormant>();
            if (comp != null)
            {
                if (!comp.Awake)
                {
                    return;
                }
            }
            else if (parent.Position.Fogged(parent.Map))
            {
                return;
            }
            if (!PropsSpawner.requiresPower || PowerOn)
            {
                ticksUntilSpawn -= interval;
                CheckShouldSpawn();
            }
        }

        public bool TryDoSpawn()
        {
            if (!parent.Spawned)
            {
                return false;
            }
            if (PropsSpawner.spawnMaxAdjacent >= 0)
            {
                int num = 0;
                for (int i = 0; i < 9; i++)
                {
                    var c = parent.Position + GenAdj.AdjacentCellsAndInside[i];
                    if (!c.InBounds(parent.Map))
                    {
                        continue;
                    }
                    var thingList = c.GetThingList(parent.Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].def == PropsSpawner.thingToSpawn)
                        {
                            num += thingList[j].stackCount;
                            if (num >= PropsSpawner.spawnMaxAdjacent)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            if (TryFindSpawnCell(parent, PropsSpawner.thingToSpawn, PropsSpawner.spawnCount, out var result))
            {
                var thing = ThingMaker.MakeThing(PropsSpawner.thingToSpawn);
                thing.stackCount = PropsSpawner.spawnCount;
                if (thing == null)
                {
                    Log.Error("Could not spawn anything for " + parent);
                }
                if (PropsSpawner.inheritFaction && thing.Faction != parent.Faction)
                {
                    thing.SetFaction(parent.Faction);
                }
                GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing);
                if (PropsSpawner.spawnForbidden)
                {
                    lastResultingThing.SetForbidden(value: true);
                }
                if (PropsSpawner.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
                {
                    Messages.Message("MessageCompSpawnerSpawnedItem".Translate(PropsSpawner.thingToSpawn.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
                }
                return true;
            }
            return false;
        }

        public static bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int spawnCount, out IntVec3 result)
        {
            foreach (var item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
            {
                if (item.Walkable(parent.Map))
                {
                    var edifice = item.GetEdifice(parent.Map);
                    if (edifice == null || !thingToSpawn.IsEdifice())
                    {
                        var building_Door = edifice as Building_Door;
                        if ((building_Door == null || building_Door.FreePassage) && (parent.def.passability == Traversability.Impassable || GenSight.LineOfSight(parent.Position, item, parent.Map)))
                        {
                            bool flag = false;
                            var thingList = item.GetThingList(parent.Map);
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                var thing = thingList[i];
                                if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - spawnCount))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                result = item;
                                return true;
                            }
                        }
                    }
                }
            }
            result = IntVec3.Invalid;
            return false;
        }

        protected void ResetCountdown()
        {
            ticksUntilSpawn = PropsSpawner.spawnIntervalRange.RandomInRange;
        }

        public override void PostExposeData()
        {
            string str = PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : (PropsSpawner.saveKeysPrefix + "_");
            Scribe_Values.Look(ref ticksUntilSpawn, str + "ticksUntilSpawn", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                var command_Action = new Command_Action
                {
                    defaultLabel = "DEBUG: Spawn " + PropsSpawner.thingToSpawn.label,
                    icon = TexCommand.DesirePower,
                    action = delegate
                    {
                        TryDoSpawn();
                        ResetCountdown();
                    }
                };
                yield return command_Action;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (PropsSpawner.writeTimeLeftToSpawn && (!PropsSpawner.requiresPower || PowerOn))
            {
                return "NextSpawnedItemIn".Translate(GenLabel.ThingLabel(PropsSpawner.thingToSpawn, null, PropsSpawner.spawnCount)) + ": " + ticksUntilSpawn.ToStringTicksToPeriod();
            }
            return null;
        }

        public virtual void CheckShouldSpawn()
        {
        }
    }
}
